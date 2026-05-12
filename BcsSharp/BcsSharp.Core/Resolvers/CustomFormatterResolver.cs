using System.Collections.Concurrent;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolver populated at runtime via <see cref="Register{T}"/>. Sits first in
/// <see cref="CompositeResolver.Default"/> so explicit registrations override any
/// other resolver, including built-ins.
/// </summary>
/// <remarks>
/// Register at application startup, before the first serialize/deserialize call.
/// <see cref="CompositeResolver"/> memoises lookups (including misses), so registering
/// after a miss has been cached will appear to do nothing. Call
/// <see cref="BcsSerializer.ClearFormatterCache"/> if you must register late (e.g. in tests).
/// </remarks>
public sealed class CustomFormatterResolver : IFormatterResolver
{
    public static readonly CustomFormatterResolver Instance = new();

    private readonly ConcurrentDictionary<Type, object> _formatters = new();

    private CustomFormatterResolver() { }

    public void Register<T>(IBcsFormatter<T> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatters[typeof(T)] = formatter;
    }

    public void Register(Type type, object formatter)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(formatter);

        var expected = typeof(IBcsFormatter<>).MakeGenericType(type);
        if (!expected.IsInstanceOfType(formatter))
        {
            throw new ArgumentException(
                $"Formatter must implement IBcsFormatter<{type.FullName}>, got {formatter.GetType().FullName}.",
                nameof(formatter));
        }

        _formatters[type] = formatter;
    }

    public bool Unregister<T>() => _formatters.TryRemove(typeof(T), out _);

    public void Clear() => _formatters.Clear();

    public IBcsFormatter<T>? GetFormatter<T>()
    {
        return _formatters.TryGetValue(typeof(T), out var formatter)
            ? (IBcsFormatter<T>)formatter
            : null;
    }
}
