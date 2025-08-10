using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers
{
    /// <summary>
    /// Resolver specifically for nullable reference types (T? where T : class)
    /// </summary>
    public sealed class NullableReferenceResolver : IFormatterResolver
    {
        public static readonly NullableReferenceResolver Instance = new();
        private static readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

        private NullableReferenceResolver() { }

        public IBcsFormatter<T>? GetFormatter<T>()
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
        }

        /// <summary>
        /// Get formatter for a specific property context (needed for nullable reference type detection)
        /// </summary>
        public IBcsFormatter<T>? GetFormatterForProperty<T>(PropertyInfo propertyInfo)
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), _ => CreateFormatterForProperty<T>(propertyInfo));
        }

        private object? CreateFormatterForProperty<T>(PropertyInfo propertyInfo)
        {
            var type = typeof(T);

            // Only handle reference types
            if (type.IsValueType)
                return null;

            // Check if this property is marked as nullable using NullableAttribute
            if (IsNullableReferenceTypeProperty(propertyInfo))
            {
                // For string, return the direct StringFormatter (we fixed this earlier)
                if (type == typeof(string))
                    return StringFormatter.Instance;

                // For other reference types, get the underlying formatter and wrap in NullableReferenceFormatter
                var underlyingFormatter = GetFormatterForUnderlyingType(type);
                if (underlyingFormatter != null)
                {
                    var nullableFormatterType = typeof(NullableReferenceFormatter<>).MakeGenericType(type);
                    return Activator.CreateInstance(nullableFormatterType, underlyingFormatter);
                }
            }

            return null;
        }

        private static object? CreateFormatter(Type type)
        {
            // Only handle reference types that could be nullable
            if (type.IsValueType)
                return null;

            // Skip generic nullable value types (T?)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            // Skip collection types - they should be handled by StandardResolver
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(Dictionary<,>) ||
                    genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(ValueTuple<,>) ||
                    genericTypeDef == typeof(ValueTuple<,,>) ||
                    genericTypeDef == typeof(ValueTuple<,,,>))
                {
                    return null;
                }
            }

            // Skip arrays
            if (type.IsArray)
                return null;

            // Check for cached common formatters first
            var cachedFormatter = GetCachedFormatter(type);
            if (cachedFormatter != null)
                return cachedFormatter;

            // For nullable reference types, we need to detect if they're actually marked as nullable
            // This is a simplified implementation - full nullable reference type detection requires
            // analyzing NullableAttribute and NullableContextAttribute metadata
            if (IsNullableReferenceType(type))
            {
                // Get the underlying formatter for the reference type
                var underlyingFormatter = GetFormatterForUnderlyingType(type);
                if (underlyingFormatter != null)
                {
                    var nullableFormatterType = typeof(NullableReferenceFormatter<>).MakeGenericType(type);
                    return Activator.CreateInstance(nullableFormatterType, underlyingFormatter);
                }
            }

            return null;
        }

        private static object? GetCachedFormatter(Type type)
        {
            // For string, return the direct StringFormatter instead of wrapping in NullableReferenceFormatter
            // This is because string already handles null values properly in BCS serialization
            if (type == typeof(string)) return StringFormatter.Instance;

            return null;
        }

        private static object? GetFormatterForUnderlyingType(Type type)
        {
            // Get formatter for the reference type from StandardResolver
            var resolverType = typeof(StandardResolver);
            var method = resolverType.GetMethod(nameof(GetFormatter))?.MakeGenericMethod(type);
            return method?.Invoke(StandardResolver.Instance, null);
        }

        /// <summary>
        /// Simplified check for nullable reference types
        /// In a complete implementation, this would analyze NullableAttribute metadata
        /// </summary>
        private static bool IsNullableReferenceType(Type type)
        {
            // This is a simplified heuristic - in practice you'd need to check:
            // 1. NullableAttribute on the type or containing member
            // 2. NullableContextAttribute on the assembly/module
            // 3. Project-level nullable context settings

            // For now, assume reference types can be nullable, including string
            return !type.IsValueType;
        }

        /// <summary>
        /// Check if a property is marked as nullable using NullableAttribute
        /// </summary>
        private static bool IsNullableReferenceTypeProperty(PropertyInfo propertyInfo)
        {
            // Check for NullableAttribute on the property
            var nullableAttribute = propertyInfo.GetCustomAttribute<NullableAttribute>();
            if (nullableAttribute != null)
            {
                // NullableAttribute has a single constructor parameter (byte[] flags)
                // flags[0]: 0 = oblivious, 1 = not null, 2 = nullable
                var flagsField = typeof(NullableAttribute).GetField("NullableFlags", BindingFlags.Public | BindingFlags.Instance);
                if (flagsField != null)
                {
                    var flags = (byte[]?)flagsField.GetValue(nullableAttribute);
                    return flags != null && flags.Length > 0 && flags[0] == 2;
                }
            }

            // If no explicit attribute, check the nullable context
            // This is a simplified implementation - full detection would require
            // analyzing NullableContextAttribute and compiler metadata
            return false;
        }
    }
}