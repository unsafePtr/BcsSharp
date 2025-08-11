using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// High-performance formatter for Rust-style variant enums (tagged unions) following the official BCS specification.
    /// Uses compiled expression trees for fast object creation and property access.
    /// Uses ULEB128-encoded variant indices and supports associated data of any BCS type.
    /// 
    /// BCS Format: [ULEB128 variant_index] + [associated_data...]
    /// Example: E::Variant2("hello") -> [2, 5, 'h', 'e', 'l', 'l', 'o']
    /// </summary>
    /// <typeparam name="T">The base enum type or interface.</typeparam>
    public sealed class BcsVariantEnumFormatter<T> : IBcsFormatter<T>, IBcsFormatter
    {
        private readonly Dictionary<uint, BcsVariantInfo> _variantsByIndex;
        private readonly Dictionary<Type, BcsVariantInfo> _variantsByType;

        // Static cache for constructor delegates to avoid recompilation - local per formatter type
        private static readonly ConcurrentDictionary<Type, Func<object>> _constructorCache = new();

        public Type TargetType { get; } = typeof(T);

        public BcsVariantEnumFormatter()
        {
            var bcsEnumAttr = TargetType.GetCustomAttribute<BcsEnumAttribute>();
            if (bcsEnumAttr == null)
                throw new InvalidOperationException($"Type {TargetType.Name} must be marked with [BcsEnum] attribute");

            var variants = DiscoverVariants(TargetType);

            _variantsByIndex = variants.ToDictionary(v => v.Index);
            _variantsByType = variants.ToDictionary(v => v.VariantType);
        }

        public void Serialize(ref BcsWriter writer, T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var valueType = value.GetType();

            if (!_variantsByType.TryGetValue(valueType, out var variant))
                throw new InvalidOperationException($"Unknown variant type: {valueType.Name}");

            // Write ULEB128 variant index (as per BCS specification)
            writer.WriteULEB(variant.Index);

            // Write associated data if present
            if (variant.DataProperties.Any())
            {
                SerializeVariantData(ref writer, value, variant);
            }
        }

        public T Deserialize(ref BcsReader reader)
        {
            // Read ULEB128 variant index
            var variantIndex = reader.ReadULEB32();

            if (!_variantsByIndex.TryGetValue(variantIndex, out var variant))
                throw new InvalidOperationException($"Unknown variant index: {variantIndex}");

            // Create instance of variant type using compiled constructor delegate
            var instance = variant.Constructor();
            if (instance == null)
                throw new InvalidOperationException($"Failed to create instance of {variant.VariantType.Name}");

            // Deserialize associated data if present
            if (variant.DataProperties.Any())
            {
                DeserializeVariantData(ref reader, instance, variant);
            }

            return (T)instance;
        }

        public int? GetSerializedSize(T value)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();

            if (!_variantsByType.TryGetValue(valueType, out var variant))
                return null;

            // Size = ULEB128 index + data size
            var indexSize = GetULEBSize(variant.Index);

            if (!variant.DataProperties.Any())
                return indexSize;

            var dataSize = CalculateVariantDataSize(value, variant);
            if (dataSize == null)
                return null;

            return indexSize + dataSize.Value;
        }

        private List<BcsVariantInfo> DiscoverVariants(Type enumBaseType)
        {
            // Strategy 1: Look for nested types first (common pattern)
            var nestedTypes = enumBaseType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            var variants = new List<BcsVariantInfo>(nestedTypes.Length);
            uint nextIndex = 0;
            foreach (var nestedType in nestedTypes)
            {
                var variantAttr = nestedType.GetCustomAttribute<BcsEnumVariantAttribute>();
                if (variantAttr == null)
                    continue;

                if (IsVariantOfEnum(nestedType, enumBaseType))
                {
                    var index = variantAttr.Index ?? nextIndex;

                    variants.Add(new BcsVariantInfo
                    {
                        Index = index,
                        Name = nestedType.Name, // Only used for debugging/diagnostics
                        VariantType = nestedType,
                        Constructor = GetOrCreateConstructorDelegate(nestedType),
                        DataProperties = DiscoverDataProperties(nestedType)
                    });

                    nextIndex = Math.Max(nextIndex, index + 1);
                }
            }

            // Strategy 2: Look for types in the same assembly (for standalone variant classes)
            if (variants.Count == 0)
            {
                var assembly = enumBaseType.Assembly;
                var allTypes = assembly.GetTypes();

                foreach (var type in allTypes)
                {
                    // Skip if it's the enum base type itself
                    if (type == enumBaseType)
                        continue;

                    var variantAttr = type.GetCustomAttribute<BcsEnumVariantAttribute>();
                    if (variantAttr == null)
                        continue;

                    // Check if type implements/inherits from the enum base type
                    if (IsVariantOfEnum(type, enumBaseType))
                    {
                        var index = variantAttr.Index ?? nextIndex;

                        variants.Add(new BcsVariantInfo
                        {
                            Index = index,
                            Name = type.Name, // Only used for debugging/diagnostics
                            VariantType = type,
                            Constructor = GetOrCreateConstructorDelegate(type),
                            DataProperties = DiscoverDataProperties(type)
                        });

                        nextIndex = Math.Max(nextIndex, index + 1);
                    }
                }
            }

            if (variants.Count == 0)
            {
                throw new InvalidOperationException($"No variants found for enum {enumBaseType.Name}. " +
                    $"Ensure variant classes are marked with [BcsEnumVariant] and implement/inherit from {enumBaseType.Name}. " +
                    $"Searched {enumBaseType.GetNestedTypes().Length} nested types and {enumBaseType.Assembly.GetTypes().Length} assembly types.");
            }

            return [.. variants.OrderBy(v => v.Index)];
        }

        private static bool IsVariantOfEnum(Type candidateType, Type enumBaseType)
        {
            // Check if the type implements the enum interface/inherits from enum base
            // Also ensure it's not a generic type definition to avoid issues with generic variants
            return enumBaseType.IsAssignableFrom(candidateType) &&
                   candidateType != enumBaseType &&
                   !candidateType.IsGenericTypeDefinition;
        }

        private List<BcsDataProperty> DiscoverDataProperties(Type variantType)
        {
            var properties = new List<BcsDataProperty>();

            foreach (var prop in variantType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var dataAttr = prop.GetCustomAttribute<BcsEnumDataAttribute>();
                if (dataAttr == null)
                    continue;

                var formatter = BcsSerializerExtensions.GetFormatter(prop.PropertyType);
                if (formatter == null)
                    throw new InvalidOperationException($"No BCS formatter found for property {prop.Name} of type {prop.PropertyType.Name}");

                properties.Add(new BcsDataProperty
                {
                    Property = prop,
                    Order = dataAttr.Order ?? properties.Count,
                    Formatter = formatter,
                    GetValue = CompilePropertyGetter(prop),
                    SetValue = CompilePropertySetter(prop)
                });
            }

            return [.. properties.OrderBy(p => p.Order)];
        }

        private static void SerializeVariantData(ref BcsWriter writer, object instance, BcsVariantInfo variant)
        {
            foreach (var dataProp in variant.DataProperties)
            {
                // Use compiled expression-based getter for optimal performance
                var value = dataProp.GetValue(instance);
                dataProp.Formatter.SerializeObject(ref writer, value);
            }
        }

        private static void DeserializeVariantData(ref BcsReader reader, object instance, BcsVariantInfo variant)
        {
            foreach (var dataProp in variant.DataProperties)
            {
                var value = dataProp.Formatter.DeserializeObject(ref reader);
                // Use compiled expression-based setter for optimal performance
                dataProp.SetValue(instance, value);
            }
        }

        private static int? CalculateVariantDataSize(object instance, BcsVariantInfo variant)
        {
            var totalSize = 0;

            foreach (var dataProp in variant.DataProperties)
            {
                // Use compiled expression-based getter for optimal performance
                var value = dataProp.GetValue(instance);
                var size = dataProp.Formatter.GetObjectSize(value);

                if (size == null)
                    return null;

                totalSize += size.Value;
            }

            return totalSize;
        }


        private static int GetULEBSize(uint value)
        {
            if (value < 0x80) return 1;
            if (value < 0x4000) return 2;
            if (value < 0x200000) return 3;
            if (value < 0x10000000) return 4;
            return 5;
        }

        /// <summary>
        /// Creates or retrieves a cached constructor delegate for fast object instantiation
        /// </summary>
        private static Func<object> GetOrCreateConstructorDelegate(Type type)
        {
            return _constructorCache.GetOrAdd(type, CreateConstructorDelegate);
        }

        /// <summary>
        /// Creates a high-performance constructor delegate using compiled expressions
        /// </summary>
        private static Func<object> CreateConstructorDelegate(Type type)
        {
            if (type.IsValueType)
            {
                // For value types, use default(T) and box it
                var defaultExpression = Expression.Default(type);
                var boxedExpression = Expression.Convert(defaultExpression, typeof(object));
                return Expression.Lambda<Func<object>>(boxedExpression).Compile();
            }
            else
            {
                // For reference types, use the parameterless constructor
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new InvalidOperationException($"Type {type.Name} must have a parameterless constructor for BCS deserialization");

                var newExpression = Expression.New(constructor);
                var convertExpression = Expression.Convert(newExpression, typeof(object));
                return Expression.Lambda<Func<object>>(convertExpression).Compile();
            }
        }

        /// <summary>
        /// Compiles a high-performance property getter using expression trees
        /// </summary>
        private static Func<object, object?> CompilePropertyGetter(PropertyInfo property)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var convertedObj = Expression.Convert(objParam, property.DeclaringType!);
            var propertyAccess = Expression.Property(convertedObj, property);
            var boxed = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<object, object?>>(boxed, objParam).Compile();
        }

        /// <summary>
        /// Compiles a high-performance property setter using expression trees
        /// </summary>
        private static Action<object, object?> CompilePropertySetter(PropertyInfo property)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var convertedObj = Expression.Convert(objParam, property.DeclaringType!);
            var convertedValue = Expression.Convert(valueParam, property.PropertyType);
            var propertyAccess = Expression.Property(convertedObj, property);
            var assignment = Expression.Assign(propertyAccess, convertedValue);
            return Expression.Lambda<Action<object, object?>>(assignment, objParam, valueParam).Compile();
        }

        private sealed class BcsVariantInfo
        {
            public uint Index { get; set; }
            public string Name { get; set; } = "";
            public Type VariantType { get; set; } = null!;
            public Func<object> Constructor { get; set; } = null!;
            public List<BcsDataProperty> DataProperties { get; set; } = new();
        }

        private sealed class BcsDataProperty
        {
            public PropertyInfo Property { get; set; } = null!;
            public int Order { get; set; }
            public IBcsObjectFormatter Formatter { get; set; } = null!;

            // Compiled expression-based accessors for maximum performance
            public Func<object, object?> GetValue { get; set; } = null!;
            public Action<object, object?> SetValue { get; set; } = null!;
        }
    }

    /// <summary>
    /// Non-generic interface for object-based BCS formatting.
    /// Used for enum variant data serialization where the type is known at runtime.
    /// </summary>
    public interface IBcsObjectFormatter
    {
        void SerializeObject(ref BcsWriter writer, object? value);
        object? DeserializeObject(ref BcsReader reader);
        int? GetObjectSize(object? value);
    }

    /// <summary>
    /// Adapter to convert generic IBcsFormatter to object-based formatter
    /// </summary>
    public class BcsObjectFormatterAdapter<T> : IBcsObjectFormatter
    {
        private readonly IBcsFormatter<T> _formatter;

        public BcsObjectFormatterAdapter(IBcsFormatter<T> formatter)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public void SerializeObject(ref BcsWriter writer, object? value)
        {
            if (value is T typedValue)
            {
                _formatter.Serialize(ref writer, typedValue);
            }
            else if (value == null && !typeof(T).IsValueType)
            {
                _formatter.Serialize(ref writer, default(T)!);
            }
            else
            {
                throw new InvalidOperationException($"Value is not of expected type {typeof(T).Name}");
            }
        }

        public object? DeserializeObject(ref BcsReader reader)
        {
            return _formatter.Deserialize(ref reader);
        }

        public int? GetObjectSize(object? value)
        {
            if (value is T typedValue)
            {
                return _formatter.GetSerializedSize(typedValue);
            }
            else if (value == null && !typeof(T).IsValueType)
            {
                return _formatter.GetSerializedSize(default(T)!);
            }
            else
            {
                throw new InvalidOperationException($"Value is not of expected type {typeof(T).Name}");
            }
        }
    }

    /// <summary>
    /// Helper extension methods for BcsSerializer to support object-based formatting
    /// </summary>
    public static class BcsSerializerExtensions
    {
        public static IBcsObjectFormatter? GetFormatter(Type type)
        {
            var method = typeof(BcsSerializer).GetMethod(nameof(BcsSerializer.GetFormatter), BindingFlags.Public | BindingFlags.Static);
            var genericMethod = method?.MakeGenericMethod(type);
            var formatter = genericMethod?.Invoke(null, new object?[] { null }); // Pass null for the optional resolver parameter

            if (formatter == null)
                return null;

            var adapterType = typeof(BcsObjectFormatterAdapter<>).MakeGenericType(type);
            return (IBcsObjectFormatter?)Activator.CreateInstance(adapterType, formatter);
        }
    }
}