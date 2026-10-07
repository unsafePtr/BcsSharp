using BcsSharp.Core.Formatters;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Decodes maps into dictionaries keyed by <see cref="CollisionResistantComparer"/>, for input that may come from an attacker.
/// It is off by default because the keyed hash costs more than the default one; opt in with <c>CompositeResolver.Create(CollisionResistantMapResolver.Instance)</c>.
/// </summary>
public sealed class CollisionResistantMapResolver : IFormatterResolver
{
    public static readonly CollisionResistantMapResolver Instance = new();

    private CollisionResistantMapResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        var type = typeof(T);
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Dictionary<,>))
        {
            return null;
        }

        var typeArgs = type.GetGenericArguments();
        var chain = root ?? CompositeResolver.Default;
        var keyFormatter = GetFormatterForType(typeArgs[0], chain);
        var valueFormatter = GetFormatterForType(typeArgs[1], chain);

        if (keyFormatter is null || valueFormatter is null)
        {
            return null;
        }

        var comparer = typeof(CollisionResistantComparer).GetMethod(nameof(CollisionResistantComparer.For))!
            .MakeGenericMethod(typeArgs[0])
            .InvokeUnwrapped(null, []);

        var formatterType = typeof(MapFormatter<,>).MakeGenericType(typeArgs);
        return (IBcsFormatter<T>)Activator.CreateInstance(formatterType, keyFormatter, valueFormatter, comparer)!;
    }

    private static object? GetFormatterForType(Type type, IFormatterResolver root)
    {
        return typeof(IFormatterResolver).GetMethod(nameof(IFormatterResolver.GetFormatter))!
            .MakeGenericMethod(type)
            .InvokeUnwrapped(root, [null]);
    }
}
