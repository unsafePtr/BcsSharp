using System;
using System.Buffers;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Core;

/// <summary>
/// Static serializer API similar to MessagePackSerializer
/// </summary>
public static class BcsSerializer
{
    private static IFormatterResolver _defaultResolver = CompositeResolver.Default;

    /// <summary>
    /// Default resolver used when none is specified
    /// </summary>
    public static IFormatterResolver DefaultResolver
    {
        get => _defaultResolver;
        set => _defaultResolver = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Serialize value to byte array.
    /// </summary>
    /// <remarks>
    /// Writes into a thread-static 64 KB scratch buffer (allocated once per thread) and
    /// falls over to <see cref="ArrayPool{T}.Shared"/> only when the payload exceeds it
    /// or another serialization on the same thread is already holding the scratch.
    /// </remarks>
    public static byte[] Serialize<T>(T value, IFormatterResolver? resolver = null)
    {
        var bufferWriter = ScratchBufferWriter.Rent();
        try
        {
            var writer = new BcsWriter(bufferWriter);
            Serialize(ref writer, value, resolver);
            return bufferWriter.WrittenSpan.ToArray();
        }
        finally
        {
            bufferWriter.Return();
        }
    }

    /// <summary>
    /// Serialize value to BcsWriter
    /// </summary>
    public static void Serialize<T>(ref BcsWriter writer, T value, IFormatterResolver? resolver = null)
    {
        resolver ??= _defaultResolver;
        var formatter = resolver.GetFormatter<T>() ?? throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
        formatter.Serialize(ref writer, value);
    }

    /// <summary>
    /// Serialize value to IBufferWriter
    /// </summary>
    public static void Serialize<T>(IBufferWriter<byte> bufferWriter, T value, IFormatterResolver? resolver = null)
    {
        var writer = new BcsWriter(bufferWriter);
        Serialize(ref writer, value, resolver);
    }

    /// <summary>
    /// Deserialize from ReadOnlyMemory
    /// </summary>
    public static T Deserialize<T>(ReadOnlyMemory<byte> data, IFormatterResolver? resolver = null)
    {
        var reader = new BcsReader(data);
        return Deserialize<T>(ref reader, resolver);
    }

    /// <summary>
    /// Deserialize from BcsReader
    /// </summary>
    public static T Deserialize<T>(ref BcsReader reader, IFormatterResolver? resolver = null)
    {
        resolver ??= _defaultResolver;
        var formatter = resolver.GetFormatter<T>() ?? throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
        return formatter.Deserialize(ref reader);
    }


    /// <summary>
    /// Get formatter for type
    /// </summary>
    public static IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? resolver = null)
    {
        resolver ??= _defaultResolver;
        return resolver.GetFormatter<T>();
    }

    /// <summary>
    /// Clears <see cref="CompositeResolver"/>'s shared lookup cache. Call after any
    /// <see cref="CustomFormatterResolver.Register{T}"/> done after the first serialize
    /// (e.g. in tests), so cached null misses don't shadow the new registration.
    /// </summary>
    public static void ClearFormatterCache() => CompositeResolver.ClearCache();
}
