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
/// <para>
/// <b>Prefer the C# 15 <c>union</c> keyword</b> for new code — handled by
/// <see cref="UnionResolver"/>, produces identical BCS wire bytes, requires no attributes,
/// gets compile-time exhaustiveness on switch expressions, and supports zero-allocation
/// cases when the payload is a reference type.
/// </para>
/// <para>
/// This resolver is kept for one specific scenario: when the variant set must expose a
/// <i>shared interface</i> for runtime polymorphism unrelated to BCS (virtual dispatch on
/// case methods, IoC mocking, visitor pattern). C# 15 unions have no shared supertype
/// across cases, so that pattern requires the older marker-interface shape.
/// </para>
/// </remarks>
public sealed class VariantEnumResolver : IFormatterResolver
{
    public static readonly VariantEnumResolver Instance = new();

    private VariantEnumResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        return (IBcsFormatter<T>?)CreateFormatter(typeof(T), root ?? this);
    }

    private static object? CreateFormatter(Type type, IFormatterResolver root)
    {
        if (type.GetCustomAttribute<BcsEnumAttribute>() is null)
        {
            return null;
        }

        var formatterType = typeof(BcsVariantEnumFormatter<>).MakeGenericType(type);
        return Activator.CreateInstance(formatterType, root);
    }
}
