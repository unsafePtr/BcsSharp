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

        private static object? CreateFormatter(Type type)
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
    }
}