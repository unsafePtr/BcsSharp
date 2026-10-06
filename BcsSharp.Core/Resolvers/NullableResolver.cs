using BcsSharp.Core.Formatters;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolver specifically for nullable value types (T?)
/// </summary>
public sealed class NullableResolver : IFormatterResolver
{
    public static readonly NullableResolver Instance = new();

    private NullableResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        return (IBcsFormatter<T>?)CreateFormatter(typeof(T), root ?? this);
    }

    private static object? CreateFormatter(Type type, IFormatterResolver root)
    {
        // Only handle nullable value types (T?)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = type.GetGenericArguments()[0];

            // Check for cached common formatters first
            var cachedFormatter = GetCachedFormatter(underlyingType);
            if (cachedFormatter != null)
            {
                return cachedFormatter;
            }

            // Fall back to dynamic creation for other types
            var underlyingFormatter = GetFormatterForUnderlyingType(underlyingType, root);
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
        if (underlyingType == typeof(byte))
        {
            return OptionFormatterCache.ByteOptionFormatter;
        }

        if (underlyingType == typeof(sbyte))
        {
            return OptionFormatterCache.SByteOptionFormatter;
        }

        if (underlyingType == typeof(ushort))
        {
            return OptionFormatterCache.UInt16OptionFormatter;
        }

        if (underlyingType == typeof(short))
        {
            return OptionFormatterCache.Int16OptionFormatter;
        }

        if (underlyingType == typeof(uint))
        {
            return OptionFormatterCache.UInt32OptionFormatter;
        }

        if (underlyingType == typeof(int))
        {
            return OptionFormatterCache.Int32OptionFormatter;
        }

        if (underlyingType == typeof(ulong))
        {
            return OptionFormatterCache.UInt64OptionFormatter;
        }

        if (underlyingType == typeof(long))
        {
            return OptionFormatterCache.Int64OptionFormatter;
        }

        if (underlyingType == typeof(UInt128))
        {
            return OptionFormatterCache.UInt128OptionFormatter;
        }

        if (underlyingType == typeof(Int128))
        {
            return OptionFormatterCache.Int128OptionFormatter;
        }

        if (underlyingType == typeof(bool))
        {
            return OptionFormatterCache.BoolOptionFormatter;
        }

        return null;
    }

    private static object? GetFormatterForUnderlyingType(Type type, IFormatterResolver root)
    {
        // Go through the full resolver chain so that [BcsStruct] value types, custom
        // attribute-registered types, etc. all work as Nullable<T> payloads — not just
        // the primitive/collection set that StandardResolver knows about. Matches the
        // delegate-to-chain pattern used by GetFormatterForType in StandardResolver.
        return typeof(IFormatterResolver).GetMethod(nameof(IFormatterResolver.GetFormatter))!
            .MakeGenericMethod(type)
            .InvokeUnwrapped(root, [null]);
    }
}
