using System;
using System.Collections.Concurrent;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolver specifically for nullable value types (T?)
/// </summary>
public sealed class NullableResolver : IFormatterResolver
{
    public static readonly NullableResolver Instance = new();
    private static readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

    private NullableResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>()
    {
        return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
    }

    private static object? CreateFormatter(Type type)
    {
        // Only handle nullable value types (T?)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = type.GetGenericArguments()[0];

            // Check for cached common formatters first
            var cachedFormatter = GetCachedFormatter(underlyingType);
            if (cachedFormatter != null)
                return cachedFormatter;

            // Fall back to dynamic creation for other types
            var underlyingFormatter = GetFormatterForUnderlyingType(underlyingType);
            if (underlyingFormatter != null)
            {
                var optionFormatterType = typeof(OptionFormatter<>).MakeGenericType(underlyingType);
                return Activator.CreateInstance(optionFormatterType, underlyingFormatter);
            }
        }

        return null;
    }

    private static object? GetCachedFormatter(Type underlyingType)
    {
        // Return cached common option formatters
        if (underlyingType == typeof(byte)) return OptionFormatterCache.ByteOptionFormatter;
        if (underlyingType == typeof(sbyte)) return OptionFormatterCache.SByteOptionFormatter;
        if (underlyingType == typeof(ushort)) return OptionFormatterCache.UInt16OptionFormatter;
        if (underlyingType == typeof(short)) return OptionFormatterCache.Int16OptionFormatter;
        if (underlyingType == typeof(uint)) return OptionFormatterCache.UInt32OptionFormatter;
        if (underlyingType == typeof(int)) return OptionFormatterCache.Int32OptionFormatter;
        if (underlyingType == typeof(ulong)) return OptionFormatterCache.UInt64OptionFormatter;
        if (underlyingType == typeof(long)) return OptionFormatterCache.Int64OptionFormatter;
        if (underlyingType == typeof(UInt128)) return OptionFormatterCache.UInt128OptionFormatter;
        if (underlyingType == typeof(Int128)) return OptionFormatterCache.Int128OptionFormatter;
        if (underlyingType == typeof(Nethermind.Int256.UInt256)) return OptionFormatterCache.UInt256OptionFormatter;
        if (underlyingType == typeof(bool)) return OptionFormatterCache.BoolOptionFormatter;

        return null;
    }

    private static object? GetFormatterForUnderlyingType(Type type)
    {
        // Get formatter for the underlying type from StandardResolver
        var resolverType = typeof(StandardResolver);
        var method = resolverType.GetMethod(nameof(GetFormatter))?.MakeGenericMethod(type);
        return method?.Invoke(StandardResolver.Instance, null);
    }
}
