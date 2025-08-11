using System.Linq.Expressions;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// High-performance formatter for BCS-serializable objects (structs/classes) marked with [BcsStruct].
    /// Uses compiled expression trees instead of reflection for optimal performance.
    /// Serializes fields/properties in the order specified by [BcsField(order)] attributes.
    /// 
    /// BCS Format: Fields are serialized consecutively in order without any headers or separators.
    /// Example: struct Person { name: String, age: u32 } -> [name_data...] + [age_data...]
    /// </summary>
    /// <typeparam name="T">The object type to serialize.</typeparam>
    public sealed class BcsObjectFormatter<T> : IBcsFormatter<T>, IBcsFormatter
    {
        private readonly List<CompiledFieldInfo<T>> _fields;
        private readonly Func<T> _constructor;
        private readonly bool _isValueType;

        public Type TargetType => TypeCached;
        private static Type TypeCached { get; } = typeof(T);

        public BcsObjectFormatter()
        {
            var bcsStructAttr = TargetType.GetCustomAttribute<BcsStructAttribute>();
            if (bcsStructAttr == null)
            {
                ThrowHelper.ThrowInvalidOperationException($"Type {TargetType.Name} must be marked with [BcsStruct] attribute");
            }

            _isValueType = TargetType.IsValueType;
            _fields = DiscoverAndCompileFields(TargetType);
            _constructor = CreateConstructorDelegate();

            if (_fields.Count == 0)
            {
                ThrowHelper.ThrowInvalidOperationException($"Type {TargetType.Name} has no serializable fields marked with [BcsField]");
            }
        }

        public void Serialize(ref BcsWriter writer, T value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // Use compiled expression-based accessors for optimal performance
            foreach (var field in _fields)
            {
                var fieldValue = field.GetValue(value);
                field.Formatter.SerializeObject(ref writer, fieldValue);
            }
        }

        public T Deserialize(ref BcsReader reader)
        {
            // Use compiled constructor delegate for fast object creation
            if (_isValueType)
            {
                // For value types, we need to use boxing to properly set field values
                // This is an inherent limitation of value types in .NET
                object boxedInstance = _constructor()!;

                foreach (var field in _fields)
                {
                    var fieldValue = field.Formatter.DeserializeObject(ref reader);
                    field.SetValueBoxed(boxedInstance!, fieldValue);
                }

                return (T)boxedInstance!;
            }
            else
            {
                // For reference types, use the compiled constructor and typed setters
                var instance = _constructor();

                foreach (var field in _fields)
                {
                    var fieldValue = field.Formatter.DeserializeObject(ref reader);
                    field.SetValue(instance, fieldValue);
                }

                return instance;
            }
        }

        public int? GetSerializedSize(T value)
        {
            if (value == null)
                return null;

            var totalSize = 0;

            foreach (var field in _fields)
            {
                var fieldValue = field.GetValue(value);
                var fieldSize = field.Formatter.GetObjectSize(fieldValue);

                if (fieldSize == null)
                    return null;

                totalSize += fieldSize.Value;
            }

            return totalSize;
        }

        /// <summary>
        /// Creates a fast constructor delegate using compiled expressions
        /// </summary>
        private static Func<T> CreateConstructorDelegate()
        {
            // Use cached constructor delegate if available
            if (typeof(T).IsValueType)
            {
                // For value types, use default(T) in a compiled expression
                var newExpression = Expression.Default(typeof(T));
                return Expression.Lambda<Func<T>>(newExpression).Compile();
            }
            else
            {
                // For reference types, use the parameterless constructor
                var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new InvalidOperationException($"Type {typeof(T).Name} must have a parameterless constructor for BCS deserialization");

                var newExpression = Expression.New(constructor);
                return Expression.Lambda<Func<T>>(newExpression).Compile();
            }
        }

        /// <summary>
        /// Discovers fields/properties and compiles high-performance accessors using expression trees
        /// </summary>
        private static List<CompiledFieldInfo<T>> DiscoverAndCompileFields(Type objectType)
        {
            var fields = new List<CompiledFieldInfo<T>>();

            // Get all fields and properties
            List<MemberInfo> members =
            [
                .. objectType.GetFields(BindingFlags.Public | BindingFlags.Instance),
                .. objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            ];

            foreach (var member in members)
            {
                var fieldAttr = member.GetCustomAttribute<BcsFieldAttribute>();
                if (fieldAttr == null)
                    continue;

                Type memberType;
                Func<T, object?> typedGetter;
                Action<T, object?> typedSetter;
                Action<object, object?> boxedSetter;

                if (member is FieldInfo field)
                {
                    memberType = field.FieldType;
                    typedGetter = CompileFieldGetter<T>(field);
                    typedSetter = CompileFieldSetter<T>(field);
                    boxedSetter = (obj, value) => field.SetValue(obj, value); // Fallback for value types
                }
                else if (member is PropertyInfo property)
                {
                    memberType = property.PropertyType;

                    if (!property.CanRead)
                        throw new InvalidOperationException($"Property {member.Name} in {objectType.Name} must be readable");
                    if (!property.CanWrite)
                        throw new InvalidOperationException($"Property {member.Name} in {objectType.Name} must be writable");

                    typedGetter = CompilePropertyGetter<T>(property);
                    typedSetter = CompilePropertySetter<T>(property);
                    boxedSetter = property.SetValue; // Fallback for value types
                }
                else
                {
                    continue; // Should never happen
                }

                var formatter = BcsSerializerExtensions.GetFormatter(memberType);
                if (formatter == null)
                    throw new InvalidOperationException($"No BCS formatter found for field {member.Name} of type {memberType.Name}");

                var fieldName = member.Name;
                var order = fieldAttr.Order;

                fields.Add(new CompiledFieldInfo<T>
                {
                    Name = fieldName,
                    MemberName = member.Name,
                    Order = order,
                    MemberType = memberType,
                    Formatter = formatter,
                    GetValue = typedGetter,
                    SetValue = typedSetter,
                    SetValueBoxed = boxedSetter
                });
            }

            // Validate that all fields have explicit ordering and no duplicates
            ValidateFieldOrdering(fields);

            // Sort fields by explicit order as specified by BCS canonical structure definition
            return [.. fields.OrderBy(f => f.Order)];
        }

        /// <summary>
        /// Validates that all fields have explicit ordering and no duplicate orders (BCS compliance)
        /// </summary>
        private static void ValidateFieldOrdering(List<CompiledFieldInfo<T>> fields)
        {
            // Check for duplicate field orders
            var orderGroups = fields.GroupBy(f => f.Order).Where(g => g.Count() > 1).ToList();
            if (orderGroups.Any())
            {
                var duplicateOrders = orderGroups.Select(g => $"Order {g.Key}: [{string.Join(", ", g.Select(f => f.Name))}]");
                throw new InvalidOperationException(
                    $"BCS struct fields must have unique [BcsField(order)] values. " +
                    $"Duplicate orders found: {string.Join("; ", duplicateOrders)}");
            }
        }

        /// <summary>
        /// Compiles a high-performance field getter using expression trees
        /// </summary>
        private static Func<TObj, object?> CompileFieldGetter<TObj>(FieldInfo field)
        {
            var param = Expression.Parameter(typeof(TObj), "obj");
            var fieldAccess = Expression.Field(param, field);
            var boxed = Expression.Convert(fieldAccess, typeof(object));
            return Expression.Lambda<Func<TObj, object?>>(boxed, param).Compile();
        }

        /// <summary>
        /// Compiles a high-performance field setter using expression trees
        /// </summary>
        private static Action<TObj, object?> CompileFieldSetter<TObj>(FieldInfo field)
        {
            var objParam = Expression.Parameter(typeof(TObj), "obj");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var fieldAccess = Expression.Field(objParam, field);
            var convertedValue = Expression.Convert(valueParam, field.FieldType);
            var assignment = Expression.Assign(fieldAccess, convertedValue);
            return Expression.Lambda<Action<TObj, object?>>(assignment, objParam, valueParam).Compile();
        }

        /// <summary>
        /// Compiles a high-performance property getter using expression trees
        /// </summary>
        private static Func<TObj, object?> CompilePropertyGetter<TObj>(PropertyInfo property)
        {
            var param = Expression.Parameter(typeof(TObj), "obj");
            var propertyAccess = Expression.Property(param, property);
            var boxed = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<TObj, object?>>(boxed, param).Compile();
        }

        /// <summary>
        /// Compiles a high-performance property setter using expression trees
        /// </summary>
        private static Action<TObj, object?> CompilePropertySetter<TObj>(PropertyInfo property)
        {
            var objParam = Expression.Parameter(typeof(TObj), "obj");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var propertyAccess = Expression.Property(objParam, property);
            var convertedValue = Expression.Convert(valueParam, property.PropertyType);
            var assignment = Expression.Assign(propertyAccess, convertedValue);
            return Expression.Lambda<Action<TObj, object?>>(assignment, objParam, valueParam).Compile();
        }

        /// <summary>
        /// High-performance compiled field information with expression-based accessors
        /// </summary>
        private sealed class CompiledFieldInfo<TObj>
        {
            public string Name { get; set; } = string.Empty;
            public string MemberName { get; set; } = string.Empty;
            public int Order { get; set; }
            public Type MemberType { get; set; } = null!;
            public IBcsObjectFormatter Formatter { get; set; } = null!;

            // Compiled expression-based accessors for maximum performance
            public Func<TObj, object?> GetValue { get; set; } = null!;
            public Action<TObj, object?> SetValue { get; set; } = null!;
            public Action<object, object?> SetValueBoxed { get; set; } = null!; // For value type boxing scenarios
        }
    }
}