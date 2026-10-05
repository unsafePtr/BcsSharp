using System.Buffers;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Core;

/// <summary>
/// Static serializer API similar to MessagePackSerializer
/// </summary>
public static class BcsSerializer
{
    /// <summary>
    /// How deep structs and enums may nest in valid BCS data; sequences, maps, tuples and options do not count.
    /// The value and counting rule follow Rust's <c>bcs::MAX_CONTAINER_DEPTH</c>.
    /// Enforced only when deserializing, where the input is untrusted; like MessagePack, serialization trusts the caller's object graph.
    /// </summary>
    public const int MaxContainerDepth = 500;

    /// <summary>
    /// The largest element count a sequence, map, string or byte vector may declare, the same as Rust's <c>bcs::MAX_SEQUENCE_LENGTH</c> (2^31 - 1).
    /// </summary>
    public const int MaxSequenceLength = int.MaxValue;

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
    /// Internally rents a buffer from <see cref="ArrayPool{T}.Shared"/> (per-thread cache as the first tier) and pools the writer wrapper instance via a thread-static slot, so the only heap allocation per call is the returned <c>byte[]</c>.
    /// </summary>
    public static byte[] Serialize<T>(T value, IFormatterResolver? resolver = null)
    {
        var bufferWriter = ScratchBufferWriter.Rent();
        try
        {
            var writer = new BcsWriter(bufferWriter);
            Serialize(ref writer, value, resolver);
            writer.Flush();

            return bufferWriter.WrittenSpan.ToArray();
        }
        finally
        {
            bufferWriter.Return();
        }
    }

    /// <summary>
    /// Serialize value to BcsWriter.
    /// The caller owns the writer, so the bytes stay uncommitted until the caller calls <see cref="BcsWriter.Flush"/>.
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
        writer.Flush();
    }

    /// <summary>
    /// Deserialize from ReadOnlyMemory.
    /// The input must hold exactly one value: leftover bytes are rejected, as Rust's <c>bcs::from_bytes</c> does, so two different byte strings never decode to the same value.
    /// </summary>
    public static T Deserialize<T>(ReadOnlyMemory<byte> data, IFormatterResolver? resolver = null)
    {
        return Deserialize<T>(data.Span, resolver);
    }

    /// <summary>
    /// Deserialize from ReadOnlySpan&lt;byte&gt;.
    /// Zero-copy entry point for callers holding a stack-allocated buffer, a slice of a larger array, or any other span source.
    /// The input must hold exactly one value: leftover bytes are rejected, as Rust's <c>bcs::from_bytes</c> does.
    /// </summary>
    public static T Deserialize<T>(ReadOnlySpan<byte> data, IFormatterResolver? resolver = null)
    {
        var reader = new BcsReader(data);
        var value = Deserialize<T>(ref reader, resolver);

        ThrowIfBytesRemain(ref reader);

        return value;
    }

    /// <summary>
    /// Deserialize from BcsReader.
    /// Bytes after the value are left unread, so a caller can decode several values from one buffer; check <see cref="BcsReader.HasRemainingBytes"/> when the input should hold just one.
    /// </summary>
    public static T Deserialize<T>(ref BcsReader reader, IFormatterResolver? resolver = null)
    {
        resolver ??= _defaultResolver;
        var formatter = resolver.GetFormatter<T>() ?? throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
        return formatter.Deserialize(ref reader);
    }

    /// <summary>
    /// In-place deserialize.
    /// Mutates <paramref name="value"/> rather than allocating: class targets reuse the existing instance (allocates only if null), collections are cleared and refilled, value-type targets are written directly into the caller's storage.
    /// Useful for pooled-message loops.
    /// Leftover bytes are rejected, as in the allocating overload.
    /// </summary>
    public static void Deserialize<T>(ReadOnlySpan<byte> data, ref T value, IFormatterResolver? resolver = null)
    {
        var reader = new BcsReader(data);
        Deserialize(ref reader, ref value, resolver);

        ThrowIfBytesRemain(ref reader);
    }

    public static void Deserialize<T>(ReadOnlyMemory<byte> data, ref T value, IFormatterResolver? resolver = null)
    {
        Deserialize(data.Span, ref value, resolver);
    }

    /// <summary>
    /// In-place deserialize from BcsReader; like the allocating reader overload, bytes after the value are left unread.
    /// </summary>
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
    /// Clears the default chain's lookup cache.
    /// Call after any <see cref="CustomFormatterResolver.Register{T}"/> done after the first serialize (e.g. in tests), so cached null misses don't shadow the new registration.
    /// Composed formatters are cached by the chain that built them, so clearing that chain drops them with it.
    /// A scoped chain from <see cref="CompositeResolver.Create"/> is unaffected — clear it via its own <see cref="CompositeResolver.Clear"/>.
    /// </summary>
    public static void ClearFormatterCache() => CompositeResolver.ClearCache();

    private static void ThrowIfBytesRemain(ref BcsReader reader)
    {
        if (reader.HasRemainingBytes)
        {
            Helpers.ThrowHelper.ThrowRemainingBytes(reader.RemainingBytes);
        }
    }
}
