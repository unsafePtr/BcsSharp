using System.Collections.Concurrent;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolves <see cref="BcsSimpleEnumFormatter{T}"/> for plain CLR enums — the natural C#
/// mapping of Rust's unit-only sum types (e.g. <c>enum AssetType { Weapon, Armor, ... }</c>).
/// Wire format: ULEB128 of the declaration index (the assigned discriminant values are ignored).
/// </summary>
public sealed class SimpleEnumResolver : IFormatterResolver
{
    public static readonly SimpleEnumResolver Instance = new();

    private readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

    private SimpleEnumResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
    }

    private static object? CreateFormatter(Type type)
    {
        if (!BcsSimpleEnumHelper.IsSimpleEnum(type))
        {
            return null;
        }

        var formatterType = typeof(BcsSimpleEnumFormatter<>).MakeGenericType(type);
        return Activator.CreateInstance(formatterType);
    }
}
