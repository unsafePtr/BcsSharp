using System;
using System.Collections.Concurrent;
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
        
        // Cached common nullable reference formatters
        public static readonly NullableReferenceFormatter<string> StringNullableFormatter = 
            new NullableReferenceFormatter<string>(StringFormatter.Instance);
        
        private NullableReferenceResolver() { }
        
        public IBcsFormatter<T>? GetFormatter<T>()
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
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
            // Return cached common nullable reference formatters
            if (type == typeof(string)) return StringNullableFormatter;
            
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
            
            // For now, assume reference types can be nullable
            return !type.IsValueType && type != typeof(string); // string handled separately
        }
    }
}