using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers
{
    /// <summary>
    /// Standard resolver that provides formatters for built-in types
    /// </summary>
    public sealed class StandardResolver : IFormatterResolver
    {
        public static readonly StandardResolver Instance = new();
        private static readonly ConcurrentDictionary<Type, object?> _formatterCache = new();
        
        private StandardResolver() { }
        
        public IBcsFormatter<T>? GetFormatter<T>()
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
        }
        
        private static object? CreateFormatter(Type type)
        {
            // Primitive types
            if (type == typeof(byte)) return ByteFormatter.Instance;
            if (type == typeof(sbyte)) return SByteFormatter.Instance;
            if (type == typeof(ushort)) return UInt16Formatter.Instance;
            if (type == typeof(short)) return Int16Formatter.Instance;
            if (type == typeof(uint)) return UInt32Formatter.Instance;
            if (type == typeof(int)) return Int32Formatter.Instance;
            if (type == typeof(ulong)) return UInt64Formatter.Instance;
            if (type == typeof(long)) return Int64Formatter.Instance;
            if (type == typeof(UInt128)) return UInt128Formatter.Instance;
            if (type == typeof(Int128)) return Int128Formatter.Instance;
            if (type == typeof(Nethermind.Int256.UInt256)) return UInt256Formatter.Instance;
            if (type == typeof(bool)) return BoolFormatter.Instance;
            if (type == typeof(string)) return StringFormatter.Instance;
            
            // Special case for byte[]
            if (type == typeof(byte[])) return ByteArrayFormatter.Instance;
            
            // Generic arrays
            if (type.IsArray && type.GetArrayRank() == 1)
            {
                var elementType = type.GetElementType()!;
                var elementFormatterType = typeof(IBcsFormatter<>).MakeGenericType(elementType);
                var elementFormatter = GetFormatterForType(elementType);
                if (elementFormatter != null)
                {
                    var arrayFormatterType = typeof(ArrayFormatter<>).MakeGenericType(elementType);
                    var getInstanceMethod = arrayFormatterType.GetMethod("GetInstance");
                    return getInstanceMethod?.Invoke(null, new[] { elementFormatter });
                }
            }
            
            // Generic Lists
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                var elementFormatter = GetFormatterForType(elementType);
                if (elementFormatter != null)
                {
                    var listFormatterType = typeof(ListFormatter<>).MakeGenericType(elementType);
                    var getInstanceMethod = listFormatterType.GetMethod("GetInstance");
                    return getInstanceMethod?.Invoke(null, new[] { elementFormatter });
                }
            }
            
            return null;
        }
        
        private static object? GetFormatterForType(Type type)
        {
            var resolverType = typeof(StandardResolver);
            var method = resolverType.GetMethod(nameof(GetFormatter))?.MakeGenericMethod(type);
            return method?.Invoke(Instance, null);
        }
    }
}