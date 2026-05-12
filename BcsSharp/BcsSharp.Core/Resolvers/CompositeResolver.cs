using System.Collections.Concurrent;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Composite resolver that combines multiple resolvers in priority order
/// </summary>
public sealed class CompositeResolver : IFormatterResolver
{
    private readonly IFormatterResolver[] _resolvers;
    private static readonly IFormatterResolver[] DefaultResolvers = [
        StandardResolver.Instance,
        NullableResolver.Instance,
        OneOfResolver.Instance,
        EnumResolver.Instance,
        ObjectResolver.Instance
    ];

    private static readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

    public CompositeResolver(params IFormatterResolver[] resolvers)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
    }

    public IBcsFormatter<T>? GetFormatter<T>()
    {
        return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), type =>
        {
            foreach (var resolver in _resolvers)
            {
                var formatter = resolver.GetFormatter<T>();
                if (formatter != null)
                    return formatter;
            }
            return null;
        });
    }

    /// <summary>
    /// Default instance with recommended resolver chain for source-generated, nullable types, OneOf types, enums, objects, and standard types
    /// </summary>
    public static readonly CompositeResolver Default = new CompositeResolver(
        CreateResolverChain()
    );

    private static IFormatterResolver[] CreateResolverChain()
    {
        var resolvers = new List<IFormatterResolver>
        {
            // [BcsFormatter(typeof(...))] on the target type — declarative cross-assembly hook.
            AttributeFormatterResolver.Instance,

            // Source generator was dropped (commit 75458b2) but the resolver hook is kept
            // so reintroducing a generator later is a no-op for callers.
            SourceGeneratedFormatterResolver.Instance,
        };

        resolvers.AddRange(DefaultResolvers);

        return resolvers.ToArray();
    }
}
