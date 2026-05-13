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
    /// Serialize value to byte array. Internally rents a buffer from
    /// <see cref="ArrayPool{T}.Shared"/> (per-thread cache as the first tier) and pools
    /// the writer wrapper instance via a thread-static slot, so the only heap allocation
    /// per call is the returned <c>byte[]</c>.
    /// </summary>
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
    /// Deserialize from ReadOnlySpan&lt;byte&gt;. Zero-copy entry point for callers holding
    /// a stack-allocated buffer, a slice of a larger array, or any other span source.
    /// </summary>
    public static T Deserialize<T>(ReadOnlySpan<byte> data, IFormatterResolver? resolver = null)
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
    /// In-place deserialize. Mutates <paramref name="value"/> rather than allocating:
    /// class targets reuse the existing instance (allocates only if null), collections
    /// are cleared and refilled, value-type targets are written directly into the
    /// caller's storage. Useful for pooled-message loops.
    /// </summary>
    public static void Deserialize<T>(ReadOnlySpan<byte> data, ref T value, IFormatterResolver? resolver = null)
    {
        var reader = new BcsReader(data);
        Deserialize(ref reader, ref value, resolver);
    }

    public static void Deserialize<T>(ReadOnlyMemory<byte> data, ref T value, IFormatterResolver? resolver = null)
    {
        var reader = new BcsReader(data);
        Deserialize(ref reader, ref value, resolver);
    }

    public static void Deserialize<T>(ref BcsReader reader, ref T value, IFormatterResolver? resolver = null)
    {
        resolver ??= _defaultResolver;
        var formatter = resolver.GetFormatter<T>() ?? throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
        formatter.Deserialize(ref reader, ref value);
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
