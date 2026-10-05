using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolver for BCS-serializable objects/structs marked with [BcsStruct].
/// Handles classes and structs that contain fields/properties marked with [BcsField].
/// </summary>
public sealed class ObjectResolver : IFormatterResolver
{
    public static readonly ObjectResolver Instance = new();

    private readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

    private ObjectResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root)
    {
        // The formatter captures a formatter per field, so it belongs to the chain that
        // built it. The root chain owns the cache.
        return (IBcsFormatter<T>?)CreateFormatter(typeof(T), root ?? this);
    }

    private static object? CreateFormatter(Type type, IFormatterResolver root)
    {
        try
        {
            // Check if it's a BCS struct (marked with [BcsStruct])
            var bcsStructAttr = type.GetCustomAttribute<BcsStructAttribute>();
            if (bcsStructAttr == null)
            {
                return null;
            }

            // Must be a class or struct
            if (!type.IsClass && !type.IsValueType)
            {
                return null;
            }

            // Must have a parameterless constructor for deserialization
            if (type.IsClass)
            {
                var constructor = type.GetConstructor(Type.EmptyTypes) ?? throw new InvalidOperationException($"Type {type.Name} must have a parameterless constructor for BCS deserialization");
            }

            // Create BcsObjectFormatter<T>
            var formatterType = typeof(BcsObjectFormatter<>).MakeGenericType(type);
            return Activator.CreateInstance(formatterType, root);
        }
        catch (Exception)
        {
            // If formatter creation fails, return null to indicate no formatter available
            return null;
        }
    }
}
