using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Composite resolver that combines multiple resolvers in priority order
/// </summary>
public sealed class CompositeResolver : IFormatterResolver
{
    private readonly IFormatterResolver[] _resolvers;
    private static readonly IFormatterResolver[] DefaultResolvers = [
        // StandardResolver intercepts Nullable<T> first and reflects into NullableResolver,
        // so NullableResolver doesn't need its own chain slot. It remains public for users
        // who compose their own chain without StandardResolver.
        StandardResolver.Instance,
        UnionResolver.Instance,
        VariantEnumResolver.Instance,
        SimpleEnumResolver.Instance,
        ObjectResolver.Instance
    ];

    private readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

    public CompositeResolver(params IFormatterResolver[] resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        // Copy so a caller mutating their array later can't desync the chain from the cache.
        _resolvers = resolvers.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        // Fast path: avoid the GetOrAdd lambda because it captures both `this` (for
        // _resolvers) and the generic parameter T, forcing a fresh delegate allocation
        // every call — ~64 B/op on the hot serialize/deserialize path.
        if (_formatterCache.TryGetValue(typeof(T), out var cached))
        {
            return (IBcsFormatter<T>?)cached;
        }

        return GetFormatterSlow<T>(root ?? this);
    }

    private IBcsFormatter<T>? GetFormatterSlow<T>(IFormatterResolver root)
    {
        foreach (var resolver in _resolvers)
        {
            var formatter = resolver.GetFormatter<T>(root);
            if (formatter != null)
            {
                _formatterCache.TryAdd(typeof(T), formatter);
                return formatter;
            }
        }

        _formatterCache.TryAdd(typeof(T), null);
        return null;
    }

    /// <summary>
    /// Default instance with recommended resolver chain for source-generated, nullable types, unions, enums, objects, and standard types
    /// </summary>
    public static readonly CompositeResolver Default = new CompositeResolver(
        CreateResolverChain()
    );

    /// <summary>
    /// Builds a chain with <paramref name="resolvers"/> ahead of the default chain, each
    /// with its own lookup cache. Use this instead of registering on
    /// <see cref="CustomFormatterResolver.Instance"/> when the override should not be
    /// visible process-wide.
    /// </summary>
    public static CompositeResolver Create(params IFormatterResolver[] resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        return new CompositeResolver([.. resolvers, .. CreateResolverChain()]);
    }

    private static IFormatterResolver[] CreateResolverChain()
    {
        var resolvers = new List<IFormatterResolver>
        {
            // Manual registrations win over everything else so users can override built-ins.
            CustomFormatterResolver.Instance,

            // [BcsFormatter(typeof(...))] on the target type — declarative cross-assembly hook.
            AttributeFormatterResolver.Instance,

            // Source generator was dropped (commit 75458b2) but the resolver hook is kept
            // so reintroducing a generator later is a no-op for callers.
            SourceGeneratedFormatterResolver.Instance,
        };

        resolvers.AddRange(DefaultResolvers);

        return resolvers.ToArray();
    }

    /// <summary>
    /// Clears this chain's formatter lookup cache. Call this after late
    /// <see cref="CustomFormatterResolver.Register{T}"/> calls so cached misses don't
    /// shadow the new registration. Intended for tests and one-shot startup wiring.
    /// </summary>
    public void Clear() => _formatterCache.Clear();

    /// <summary>Clears <see cref="Default"/>'s cache.</summary>
    public static void ClearCache() => Default.Clear();
}
