using System.Collections.Concurrent;
using System.Reflection;
using Nethermind.Int256;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// Marker interface that can be replaced by source generator in the future
    /// </summary>
    public interface IBcsSerializable
    {
        // Empty marker interface - source generator will implement serialization methods
    }

    /// <summary>
    /// Auto-discovery struct type that serializes properties in declaration order
    /// Uses reflection now, but designed to be replaced by source generator later
    /// </summary>
    public class AutoStructType<T> : BcsType<T> where T : class, new()
    {
        private readonly List<Types.PropertySerializer> _propertySerializers;

        public AutoStructType() : base(typeof(T).Name)
        {
            _propertySerializers = DiscoverProperties();
        }

        public AutoStructType(string name) : base(name)
        {
            _propertySerializers = DiscoverProperties();
        }

        public override T Read(BcsReader reader)
        {
            var result = new T();

            foreach (var serializer in _propertySerializers)
            {
                serializer.Read(result, reader);
            }

            return result;
        }

        public override void Write(T value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            foreach (var serializer in _propertySerializers)
            {
                serializer.Write(value, writer);
            }
        }

        public override int? SerializedSize(T value)
        {
            if (value == null) return null;

            int totalSize = 0;

            foreach (var serializer in _propertySerializers)
            {
                var propertySize = serializer.GetSize(value);
                if (propertySize == null)
                    return null;

                totalSize += propertySize.Value;
            }

            return totalSize;
        }

        /// <summary>
        /// Discover properties using reflection - this will be replaced by source generator
        /// </summary>
        private static List<Types.PropertySerializer> DiscoverProperties()
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .OrderBy(p => p.MetadataToken) // Declaration order (mostly reliable)
                .ToArray();

            var serializers = new List<Types.PropertySerializer>();

            foreach (var property in properties)
            {
                var serializer = CreatePropertySerializer(property);
                if (serializer != null)
                {
                    serializers.Add(serializer);
                }
            }

            return serializers;
        }

        /// <summary>
        /// Cached property serializer factories for basic types - reuse existing BCS instances
        /// </summary>
        private static readonly Dictionary<Type, Func<PropertyInfo, Types.PropertySerializer?>> SerializerFactories
            = new(new Dictionary<Type, Func<PropertyInfo, Types.PropertySerializer?>>
            {
                // Unsigned integers - reuse existing static instances
                [typeof(byte)] = prop => new Types.TypedPropertySerializer<byte>(prop, Bcs.U8),
                [typeof(ushort)] = prop => new Types.TypedPropertySerializer<ushort>(prop, Bcs.U16),
                [typeof(uint)] = prop => new Types.TypedPropertySerializer<uint>(prop, Bcs.U32),
                [typeof(ulong)] = prop => new Types.TypedPropertySerializer<ulong>(prop, Bcs.U64),
                [typeof(UInt128)] = prop => new Types.TypedPropertySerializer<UInt128>(prop, Bcs.U128),
                [typeof(UInt256)] = prop => new Types.TypedPropertySerializer<UInt256>(prop, Bcs.U256),

                // Signed integers - reuse existing static instances  
                [typeof(sbyte)] = prop => new Types.TypedPropertySerializer<sbyte>(prop, Bcs.I8),
                [typeof(short)] = prop => new Types.TypedPropertySerializer<short>(prop, Bcs.I16),
                [typeof(int)] = prop => new Types.TypedPropertySerializer<int>(prop, Bcs.I32),
                [typeof(long)] = prop => new Types.TypedPropertySerializer<long>(prop, Bcs.I64),
                [typeof(Int128)] = prop => new Types.TypedPropertySerializer<Int128>(prop, Bcs.I128),

                // Other basic types - reuse existing static instances
                [typeof(bool)] = prop => new Types.TypedPropertySerializer<bool>(prop, Bcs.Bool),
                [typeof(string)] = prop => new Types.TypedPropertySerializer<string>(prop, Bcs.String),

                // All array types - cached vector instances to avoid runtime creation
                [typeof(byte[])] = prop => new Types.TypedPropertySerializer<byte[]>(prop, Bcs.Vector(Bcs.U8)),
                [typeof(ushort[])] = prop => new Types.TypedPropertySerializer<ushort[]>(prop, Bcs.Vector(Bcs.U16)),
                [typeof(uint[])] = prop => new Types.TypedPropertySerializer<uint[]>(prop, Bcs.Vector(Bcs.U32)),
                [typeof(ulong[])] = prop => new Types.TypedPropertySerializer<ulong[]>(prop, Bcs.Vector(Bcs.U64)),
                [typeof(UInt128[])] = prop => new Types.TypedPropertySerializer<UInt128[]>(prop, Bcs.Vector(Bcs.U128)),
                [typeof(UInt256[])] = prop => new Types.TypedPropertySerializer<UInt256[]>(prop, Bcs.Vector(Bcs.U256)),
                [typeof(sbyte[])] = prop => new Types.TypedPropertySerializer<sbyte[]>(prop, Bcs.Vector(Bcs.I8)),
                [typeof(short[])] = prop => new Types.TypedPropertySerializer<short[]>(prop, Bcs.Vector(Bcs.I16)),
                [typeof(int[])] = prop => new Types.TypedPropertySerializer<int[]>(prop, Bcs.Vector(Bcs.I32)),
                [typeof(long[])] = prop => new Types.TypedPropertySerializer<long[]>(prop, Bcs.Vector(Bcs.I64)),
                [typeof(Int128[])] = prop => new Types.TypedPropertySerializer<Int128[]>(prop, Bcs.Vector(Bcs.I128)),
                [typeof(bool[])] = prop => new Types.TypedPropertySerializer<bool[]>(prop, Bcs.Vector(Bcs.Bool)),
                [typeof(string[])] = prop => new Types.TypedPropertySerializer<string[]>(prop, Bcs.Vector(Bcs.String)),
            });

        /// <summary>
        /// Create property serializer - uses cached factories for basic types, creates new for complex types
        /// </summary>
        private static Types.PropertySerializer? CreatePropertySerializer(PropertyInfo property)
        {
            var propertyType = property.PropertyType;

            // Try cached basic types first (fast path)
            if (SerializerFactories.TryGetValue(propertyType, out var factory))
            {
                return factory(property);
            }

            // Handle C# enums as byte values (BCS standard)
            if (propertyType.IsEnum)
            {
                return CreateEnumPropertySerializer(property, propertyType);
            }

            // Handle nested custom classes directly
            if (propertyType.IsClass && propertyType != typeof(string) && !propertyType.IsArray)
            {
                var structType = GetOrCreateCachedStructType(propertyType);
                if (structType != null)
                {
                    return CreateGenericPropertySerializer(property, propertyType, structType);
                }
            }

            // Handle complex types that need dynamic creation
            return CreateComplexPropertySerializer(property, propertyType);
        }

        /// <summary>
        /// Cache for complex BCS types to avoid recreating them
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> ComplexTypeCache = new();

        /// <summary>
        /// Create property serializers for complex types - only called for non-basic types
        /// Uses heavy caching for nested structs and complex types
        /// </summary>
        private static Types.PropertySerializer? CreateComplexPropertySerializer(PropertyInfo property, Type propertyType)
        {
            // Arrays of custom classes (nested structs) with caching
            if (propertyType.IsArray)
            {
                var elementType = propertyType.GetElementType()!;

                // Only handle arrays of custom classes - basic arrays are in the main cache
                if (elementType.IsClass && elementType != typeof(string))
                {
                    var vectorType = GetOrCreateCachedVectorType(elementType);
                    if (vectorType != null)
                    {
                        return CreateGenericPropertySerializer(property, propertyType, vectorType);
                    }
                }
            }

            // Handle nullable types (Option<T>)
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(propertyType)!;
                var optionType = GetOrCreateCachedOptionType(underlyingType);
                if (optionType != null)
                {
                    return CreateGenericPropertySerializer(property, propertyType, optionType);
                }
            }

            // Handle generic collections (List<T>, etc.)
            if (propertyType.IsGenericType)
            {
                var genericTypeDef = propertyType.GetGenericTypeDefinition();
                var elementType = propertyType.GetGenericArguments()[0];

                if (genericTypeDef == typeof(List<>) || genericTypeDef == typeof(IList<>) || genericTypeDef == typeof(ICollection<>))
                {
                    var vectorType = GetOrCreateCachedVectorType(elementType);
                    if (vectorType != null)
                    {
                        return CreateGenericPropertySerializer(property, propertyType, vectorType);
                    }
                }
            }

            // Handle custom classes (nested BCS structs) with caching
            if (propertyType.IsClass && propertyType != typeof(string))
            {
                var structType = GetOrCreateCachedStructType(propertyType);
                if (structType != null)
                {
                    return CreateGenericPropertySerializer(property, propertyType, structType);
                }
            }

            return null; // Unsupported complex type
        }

        /// <summary>
        /// Get or create cached vector type for given element type
        /// </summary>
        private static object? GetOrCreateCachedVectorType(Type elementType)
        {
            var vectorTypeKey = typeof(VectorType<>).MakeGenericType(elementType);
            return ComplexTypeCache.GetOrAdd(vectorTypeKey, _ =>
            {
                // Try to get basic BCS type for element
                var elementBcsType = GetBcsTypeForType(elementType);
                if (elementBcsType == null) return null!;

                // Create vector type using reflection with proper generic method
                var vectorMethod = typeof(Bcs).GetMethods()
                    .FirstOrDefault(m => m.Name == nameof(Bcs.Vector) && m.IsGenericMethodDefinition);
                if (vectorMethod == null) return null!;

                var genericVectorMethod = vectorMethod.MakeGenericMethod(elementType);
                return genericVectorMethod.Invoke(null, new[] { elementBcsType })!;
            });
        }

        /// <summary>
        /// Get or create cached option type for given element type
        /// </summary>
        private static object? GetOrCreateCachedOptionType(Type elementType)
        {
            var optionTypeKey = typeof(OptionType<>).MakeGenericType(elementType);
            return ComplexTypeCache.GetOrAdd(optionTypeKey, _ =>
            {
                // Try to get basic BCS type for element
                var elementBcsType = GetBcsTypeForType(elementType);
                if (elementBcsType == null) return null!;

                // Create option type using reflection with proper generic method
                var optionMethod = typeof(Bcs).GetMethods()
                    .FirstOrDefault(m => m.Name == nameof(Bcs.Option) && m.IsGenericMethodDefinition);
                if (optionMethod == null) return null!;

                var genericOptionMethod = optionMethod.MakeGenericMethod(elementType);
                return genericOptionMethod.Invoke(null, new[] { elementBcsType })!;
            });
        }

        /// <summary>
        /// Get or create cached struct type for given class type
        /// </summary>
        private static object? GetOrCreateCachedStructType(Type structType)
        {
            return ComplexTypeCache.GetOrAdd(structType, _ =>
            {
                // Create AutoStructType<T> using reflection
                var autoStructType = typeof(AutoStructType<>).MakeGenericType(structType);
                var instance = Activator.CreateInstance(autoStructType)!;
                return instance;
            });
        }

        /// <summary>
        /// Get BCS type instance for basic types - reuses existing static instances
        /// </summary>
        private static object? GetBcsTypeForType(Type type)
        {
            // Reuse existing static instances for basic types
            if (type == typeof(byte)) return Bcs.U8;
            if (type == typeof(ushort)) return Bcs.U16;
            if (type == typeof(uint)) return Bcs.U32;
            if (type == typeof(ulong)) return Bcs.U64;
            if (type == typeof(UInt128)) return Bcs.U128;
            if (type == typeof(UInt256)) return Bcs.U256;
            if (type == typeof(sbyte)) return Bcs.I8;
            if (type == typeof(short)) return Bcs.I16;
            if (type == typeof(int)) return Bcs.I32;
            if (type == typeof(long)) return Bcs.I64;
            if (type == typeof(Int128)) return Bcs.I128;
            if (type == typeof(bool)) return Bcs.Bool;
            if (type == typeof(string)) return Bcs.String;

            // For custom classes, try to create auto struct type
            if (type.IsClass)
            {
                return GetOrCreateCachedStructType(type);
            }

            return null; // Unsupported type
        }

        /// <summary>
        /// Create generic property serializer using reflection for complex types
        /// FIXED: Use external PropertySerializer to avoid double-generic reflection issues
        /// </summary>
        private static Types.PropertySerializer? CreateGenericPropertySerializer(PropertyInfo property, Type propertyType, object bcsTypeInstance)
        {
            return new Types.GenericPropertySerializer(property, bcsTypeInstance);
        }

        /// <summary>
        /// Create property serializer for C# enums - treats them as byte values per BCS standard
        /// </summary>
        private static Types.PropertySerializer? CreateEnumPropertySerializer(PropertyInfo property, Type enumType)
        {
            return new Types.EnumPropertySerializer(property, enumType);
        }
    }

    // Factory methods moved to BcsStruct class in StructType.cs for unified API
}