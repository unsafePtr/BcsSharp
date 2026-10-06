using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolver for BCS-serializable objects/structs marked with [BcsStruct].
/// Handles classes and structs that contain fields/properties marked with [BcsField].
/// </summary>
public sealed class ObjectResolver : IFormatterResolver
{
    public static readonly ObjectResolver Instance = new();

    private ObjectResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        // The formatter captures a formatter per field, so it belongs to the chain that
        // built it. The root chain owns the cache.
        return (IBcsFormatter<T>?)CreateFormatter(typeof(T), root ?? this);
    }

    private static object? CreateFormatter(Type type, IFormatterResolver root)
    {
        if (type.GetCustomAttribute<BcsStructAttribute>() is null)
        {
            return null;
        }

        // A misconfigured struct must fail with the message that names the mistake, not as a missing formatter.
        return ReflectionHelper.CreateInstanceUnwrapped(typeof(BcsObjectFormatter<>).MakeGenericType(type), root);
    }
}
