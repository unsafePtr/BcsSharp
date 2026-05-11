using System.Collections.Concurrent;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// Global cache for formatter instances to avoid the issue with static fields in generic types
/// where each closed generic type gets its own static field copy
/// </summary>
internal static class FormatterCache
{
    /// <summary>
    /// Cache for formatter instances
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object> _formatterCache = new();

    /// <summary>
    /// Get or create a formatter instance for the specified type
    /// </summary>
    public static T GetOrAddFormatter<T>(Type key, Func<Type, T> factory) where T : class
    {
        return (T)_formatterCache.GetOrAdd(key, t => factory(t)!);
    }
}
