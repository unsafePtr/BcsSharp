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
            
            // Unit type
            if (type == typeof(Unit)) return UnitFormatter.Instance;

            // Nullable value types (T?) - delegate to NullableResolver
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var method = typeof(NullableResolver).GetMethod(nameof(NullableResolver.GetFormatter))?.MakeGenericMethod(type);
                return method?.Invoke(NullableResolver.Instance, null);
            }

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

            // Generic Dictionaries/Maps
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var typeArgs = type.GetGenericArguments();
                var keyType = typeArgs[0];
                var valueType = typeArgs[1];
                
                // Key must implement IComparable<TKey> for BCS maps
                var comparableInterface = typeof(IComparable<>).MakeGenericType(keyType);
                if (comparableInterface.IsAssignableFrom(keyType))
                {
                    var keyFormatter = GetFormatterForType(keyType);
                    var valueFormatter = GetFormatterForType(valueType);
                    
                    if (keyFormatter != null && valueFormatter != null)
                    {
                        var mapFormatterType = typeof(MapFormatter<,>).MakeGenericType(keyType, valueType);
                        return Activator.CreateInstance(mapFormatterType, keyFormatter, valueFormatter);
                    }
                }
            }

            // Tuples (ValueTuple)
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var typeArgs = type.GetGenericArguments();
                
                if (genericTypeDef == typeof(ValueTuple<,>) && typeArgs.Length == 2)
                {
                    var formatter1 = GetFormatterForType(typeArgs[0]);
                    var formatter2 = GetFormatterForType(typeArgs[1]);
                    
                    if (formatter1 != null && formatter2 != null)
                    {
                        var tupleFormatterType = typeof(TupleFormatter<,>).MakeGenericType(typeArgs[0], typeArgs[1]);
                        return Activator.CreateInstance(tupleFormatterType, formatter1, formatter2);
                    }
                }
                else if (genericTypeDef == typeof(ValueTuple<,,>) && typeArgs.Length == 3)
                {
                    var formatter1 = GetFormatterForType(typeArgs[0]);
                    var formatter2 = GetFormatterForType(typeArgs[1]);
                    var formatter3 = GetFormatterForType(typeArgs[2]);
                    
                    if (formatter1 != null && formatter2 != null && formatter3 != null)
                    {
                        var tupleFormatterType = typeof(TupleFormatter<,,>).MakeGenericType(typeArgs[0], typeArgs[1], typeArgs[2]);
                        return Activator.CreateInstance(tupleFormatterType, formatter1, formatter2, formatter3);
                    }
                }
                else if (genericTypeDef == typeof(ValueTuple<,,,>) && typeArgs.Length == 4)
                {
                    var formatter1 = GetFormatterForType(typeArgs[0]);
                    var formatter2 = GetFormatterForType(typeArgs[1]);
                    var formatter3 = GetFormatterForType(typeArgs[2]);
                    var formatter4 = GetFormatterForType(typeArgs[3]);
                    
                    if (formatter1 != null && formatter2 != null && formatter3 != null && formatter4 != null)
                    {
                        var tupleFormatterType = typeof(TupleFormatter<,,,>).MakeGenericType(typeArgs[0], typeArgs[1], typeArgs[2], typeArgs[3]);
                        return Activator.CreateInstance(tupleFormatterType, formatter1, formatter2, formatter3, formatter4);
                    }
                }
            }

            return null;
        }

        private static object? GetFormatterForType(Type type)
        {
            // Use the default CompositeResolver to ensure all formatters (including custom ones) are available
            // This prevents circular dependency issues when arrays/collections contain custom types
            var resolverType = typeof(IFormatterResolver);
            var method = resolverType.GetMethod(nameof(IFormatterResolver.GetFormatter))?.MakeGenericMethod(type);
            return method?.Invoke(CompositeResolver.Default, null);
        }
    }
}