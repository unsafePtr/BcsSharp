using System;
using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers
{
    /// <summary>
    /// Resolver for BCS-compatible enums. Handles both:
    /// 1. Simple C-style enums (integer-backed) 
    /// 2. Rust-style variant enums (tagged unions) marked with [BcsEnum]
    /// </summary>
    public sealed class EnumResolver : IFormatterResolver
    {
        public static readonly EnumResolver Instance = new();

        private readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

        private EnumResolver() { }

        public IBcsFormatter<T>? GetFormatter<T>()
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
        }

        private object? CreateFormatter(Type type)
        {
            // Check if it's a Rust-style variant enum (marked with [BcsEnum])
            var bcsEnumAttr = type.GetCustomAttribute<BcsEnumAttribute>();
            if (bcsEnumAttr != null)
            {
                // Create BcsVariantEnumFormatter<T> for tagged unions
                var variantFormatterType = typeof(BcsVariantEnumFormatter<>).MakeGenericType(type);
                return Activator.CreateInstance(variantFormatterType);
            }

            // Check if it's a simple C-style enum
            if (BcsSimpleEnumHelper.IsSimpleEnum(type))
            {
                // Create BcsSimpleEnumFormatter<T> for C-style enums
                var simpleFormatterType = typeof(BcsSimpleEnumFormatter<>).MakeGenericType(type);
                return Activator.CreateInstance(simpleFormatterType);
            }

            // Not an enum we can handle
            return null;
        }

        /// <summary>
        /// Checks if the resolver can handle the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a BCS-compatible enum.</returns>
        public bool CanResolve(Type type)
        {
            return type.GetCustomAttribute<BcsEnumAttribute>() != null || BcsSimpleEnumHelper.IsSimpleEnum(type);
        }

        /// <summary>
        /// Gets all supported enum types that have been cached.
        /// </summary>
        public Type[] GetCachedEnumTypes()
        {
            var types = new Type[_formatterCache.Count];
            var i = 0;
            foreach (var kvp in _formatterCache)
            {
                if (kvp.Value != null) // Only include successfully created formatters
                {
                    types[i++] = kvp.Key;
                }
            }
            
            // Resize array to actual count
            if (i < types.Length)
            {
                var actualTypes = new Type[i];
                Array.Copy(types, actualTypes, i);
                return actualTypes;
            }
            
            return types;
        }

        /// <summary>
        /// Clears the formatter cache. Useful for testing or dynamic enum registration.
        /// </summary>
        public void ClearCache()
        {
            _formatterCache.Clear();
        }
    }
}