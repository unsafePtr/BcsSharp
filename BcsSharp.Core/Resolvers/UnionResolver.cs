using System.Reflection;
using System.Runtime.CompilerServices;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolves <see cref="UnionFormatter{T}"/> for C# 15 union types — any type carrying <see cref="UnionAttribute"/> (which the compiler stamps on every <c>union</c> declaration).
/// </summary>
public sealed class UnionResolver : IFormatterResolver
{
    public static readonly UnionResolver Instance = new();

    private UnionResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root) =>
        (IBcsFormatter<T>?)CreateFormatter(typeof(T), root ?? this);

    private static object? CreateFormatter(Type type, IFormatterResolver root)
    {
        if (type.GetCustomAttribute<UnionAttribute>() is null)
        {
            return null;
        }

        return ReflectionHelper.CreateInstanceUnwrapped(typeof(UnionFormatter<>).MakeGenericType(type), root);
    }
}
