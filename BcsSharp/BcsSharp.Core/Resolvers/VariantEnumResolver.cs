using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolves <see cref="BcsVariantEnumFormatter{T}"/> for the legacy
/// <see cref="BcsEnumAttribute"/>-marked variant pattern (marker interface plus concrete
/// variant classes with <see cref="BcsEnumVariantAttribute"/> / <see cref="BcsEnumDataAttribute"/>).
/// </summary>
/// <remarks>
/// Prefer the C# 15 <c>union</c> keyword (handled by <see cref="UnionResolver"/>) for new
/// code. This resolver remains useful when the variant set needs a shared interface for
/// runtime polymorphism beyond just BCS serialization — the union path has no shared
/// supertype across cases.
/// </remarks>
public sealed class VariantEnumResolver : IFormatterResolver
{
    public static readonly VariantEnumResolver Instance = new();

    private readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

    private VariantEnumResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>()
    {
        return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
    }

    private static object? CreateFormatter(Type type)
    {
        if (type.GetCustomAttribute<BcsEnumAttribute>() is null)
        {
            return null;
        }

        var formatterType = typeof(BcsVariantEnumFormatter<>).MakeGenericType(type);
        return Activator.CreateInstance(formatterType);
    }
}
