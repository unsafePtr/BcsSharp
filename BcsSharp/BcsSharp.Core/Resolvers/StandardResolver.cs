using System;
using System.Collections.Generic;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Standard resolver that provides formatters for built-in types
/// </summary>
public sealed class StandardResolver : IFormatterResolver
{
    public static readonly StandardResolver Instance = new();

    private StandardResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        // Composed formatters (list/map/tuple) capture their children, so they are only
        // valid for the chain that supplied them. The root chain owns that cache; caching
        // here by type alone would serve one chain's children to another.
        return (IBcsFormatter<T>?)CreateFormatter(typeof(T), root ?? this);
    }

    private static object? CreateFormatter(Type type, IFormatterResolver root)
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
        if (type == typeof(bool)) return BoolFormatter.Instance;
        if (type == typeof(string)) return StringFormatter.Instance;

        // Unit type
        if (type == typeof(Unit)) return UnitFormatter.Instance;

        // Nullable value types (T?) - delegate to NullableResolver
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var method = typeof(NullableResolver).GetMethod(nameof(NullableResolver.GetFormatter))?.MakeGenericMethod(type);
            return method?.Invoke(NullableResolver.Instance, [root]);
        }

        // Generic arrays
        if (type.IsArray)
        {
            ThrowHelper.ThrowInvalidOperationException("It's not allowed to use generic arrays in BCS serialization unless it's a custom type with dedicated formatter. Use List<T> instead");
        }

        // Generic Lists
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];

            // Use specialized primitive formatter for primitive types
            if (IsPrimitiveType(elementType))
            {
                return CreatePrimitiveListFormatter(elementType);
            }
            else
            {
                var elementFormatter = GetFormatterForType(elementType, root);
                if (elementFormatter != null)
                {
                    return CreateListFormatter(elementType, elementFormatter);
                }
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
                var keyFormatter = GetFormatterForType(keyType, root);
                var valueFormatter = GetFormatterForType(valueType, root);

                if (keyFormatter != null && valueFormatter != null)
                {
                    return CreateMapFormatter(keyType, valueType, keyFormatter, valueFormatter);
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
                var formatter1 = GetFormatterForType(typeArgs[0], root);
                var formatter2 = GetFormatterForType(typeArgs[1], root);

                if (formatter1 != null && formatter2 != null)
                {
                    return CreateTuple2Formatter(typeArgs[0], typeArgs[1], formatter1, formatter2);
                }
            }
            else if (genericTypeDef == typeof(ValueTuple<,,>) && typeArgs.Length == 3)
            {
                var formatter1 = GetFormatterForType(typeArgs[0], root);
                var formatter2 = GetFormatterForType(typeArgs[1], root);
                var formatter3 = GetFormatterForType(typeArgs[2], root);

                if (formatter1 != null && formatter2 != null && formatter3 != null)
                {
                    return CreateTuple3Formatter(typeArgs[0], typeArgs[1], typeArgs[2], formatter1, formatter2, formatter3);
                }
            }
            else if (genericTypeDef == typeof(ValueTuple<,,,>) && typeArgs.Length == 4)
            {
                var formatter1 = GetFormatterForType(typeArgs[0], root);
                var formatter2 = GetFormatterForType(typeArgs[1], root);
                var formatter3 = GetFormatterForType(typeArgs[2], root);
                var formatter4 = GetFormatterForType(typeArgs[3], root);

                if (formatter1 != null && formatter2 != null && formatter3 != null && formatter4 != null)
                {
                    return CreateTuple4Formatter(typeArgs[0], typeArgs[1], typeArgs[2], typeArgs[3], formatter1, formatter2, formatter3, formatter4);
                }
            }
        }

        return null;
    }

    private static object? GetFormatterForType(Type type, IFormatterResolver root)
    {
        // Resolve children through the chain that started resolution, so a scoped chain
        // does not silently fall back to the global default.
        var method = typeof(IFormatterResolver).GetMethod(nameof(IFormatterResolver.GetFormatter))?.MakeGenericMethod(type);
        return method?.Invoke(root, [null]);
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(ushort) || type == typeof(short) ||
               type == typeof(uint) || type == typeof(int) ||
               type == typeof(ulong) || type == typeof(long) ||
               type == typeof(UInt128) || type == typeof(Int128);
    }

    private static object CreatePrimitiveListFormatter(Type elementType)
    {
        var formatterType = typeof(PrimitiveListFormatter<>).MakeGenericType(elementType);
        return Activator.CreateInstance(formatterType)!;
    }

    private static object CreateListFormatter(Type elementType, object elementFormatter)
    {
        var formatterType = typeof(ListFormatter<>).MakeGenericType(elementType);
        return Activator.CreateInstance(formatterType, elementFormatter)!;
    }

    private static object CreateMapFormatter(Type keyType, Type valueType, object keyFormatter, object valueFormatter)
    {
        var mapFormatterType = typeof(MapFormatter<,>).MakeGenericType(keyType, valueType);
        return Activator.CreateInstance(mapFormatterType, keyFormatter, valueFormatter)!;
    }

    private static object CreateTuple2Formatter(Type type1, Type type2, object formatter1, object formatter2)
    {
        var tupleFormatterType = typeof(TupleFormatter<,>).MakeGenericType(type1, type2);
        return Activator.CreateInstance(tupleFormatterType, formatter1, formatter2)!;
    }

    private static object CreateTuple3Formatter(Type type1, Type type2, Type type3, object formatter1, object formatter2, object formatter3)
    {
        var tupleFormatterType = typeof(TupleFormatter<,,>).MakeGenericType(type1, type2, type3);
        return Activator.CreateInstance(tupleFormatterType, formatter1, formatter2, formatter3)!;
    }

    private static object CreateTuple4Formatter(Type type1, Type type2, Type type3, Type type4, object formatter1, object formatter2, object formatter3, object formatter4)
    {
        var tupleFormatterType = typeof(TupleFormatter<,,,>).MakeGenericType(type1, type2, type3, type4);
        return Activator.CreateInstance(tupleFormatterType, formatter1, formatter2, formatter3, formatter4)!;
    }
}
