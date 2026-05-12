using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolves <see cref="UnionFormatter{T}"/> for C# 15 union types — any type carrying
/// <see cref="UnionAttribute"/> (which the compiler stamps on every <c>union</c> declaration).
/// </summary>
public sealed class UnionResolver : IFormatterResolver
{
    public static readonly UnionResolver Instance = new();

    private static readonly ConcurrentDictionary<Type, object?> _cache = new();

    private UnionResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>() =>
        (IBcsFormatter<T>?)_cache.GetOrAdd(typeof(T), CreateFormatter);

    private static object? CreateFormatter(Type type)
    {
        if (type.GetCustomAttribute<UnionAttribute>() is null)
        {
            return null;
        }

        var formatterType = typeof(UnionFormatter<>).MakeGenericType(type);
        return Activator.CreateInstance(formatterType);
    }
}
