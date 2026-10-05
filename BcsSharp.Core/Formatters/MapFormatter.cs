using System.Buffers;
using System.Runtime.CompilerServices;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// Formatter for Dictionary/Map types in BCS format.
/// Keys are sorted by lexicographical order of their BCS-serialized bytes for deterministic output — matches Rust's <c>bcs::ser::MapSerializer</c>, which re-sorts pairs by serialized key bytes regardless of the source container's iteration order.
/// Two keys with the same bytes are rejected, where Rust keeps the first: dictionary iteration order is undefined, so "the first" would be arbitrary.
/// </summary>
public sealed class MapFormatter<TKey, TValue> : IBcsFormatter<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <summary>Threshold below which pair-offset metadata is stack-allocated.</summary>
    private const int StackThreshold = 32;

    private readonly IBcsFormatter<TKey> _keyFormatter;
    private readonly IBcsFormatter<TValue> _valueFormatter;

    public MapFormatter(IBcsFormatter<TKey> keyFormatter, IBcsFormatter<TValue> valueFormatter)
    {
        _keyFormatter = keyFormatter ?? throw new ArgumentNullException(nameof(keyFormatter));
        _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
    }

    public void Serialize(ref BcsWriter writer, Dictionary<TKey, TValue> value)
    {
        if (value == null)
        {
            writer.WriteULEB(0u);
            return;
        }

        var count = value.Count;
        writer.WriteULEB((uint)count);
        if (count == 0)
        {
            return;
        }

        // Reuse the per-thread scratch wrapper. If the outer Serialize is already holding
        // it (the byte[]-returning overload does), Rent() hands back a fresh wrapper with
        // its own pooled buffer, so a nested map never shares scratch with its parent.
        var scratch = ScratchBufferWriter.Rent();
        PairOffsets[]? rentedPairs = null;

        try
        {
            // Stack-allocate the pair index for small maps; rent from pool for large ones.
            // `scoped` tells the compiler this span doesn't escape the method, so the
            // conditional stackalloc is legal.
            scoped Span<PairOffsets> pairs;
            if (count <= StackThreshold)
            {
                pairs = stackalloc PairOffsets[count];
            }
            else
            {
                rentedPairs = ArrayPool<PairOffsets>.Shared.Rent(count);
                pairs = rentedPairs.AsSpan(0, count);
            }

            // Serialize every (key, value) into the shared scratch, tracking offsets.
            var scratchWriter = new BcsWriter(scratch);
            var i = 0;
            foreach (var kvp in value)
            {
                var keyStart = scratchWriter.WrittenCount;
                _keyFormatter.Serialize(ref scratchWriter, kvp.Key);
                var keyEnd = scratchWriter.WrittenCount;

                _valueFormatter.Serialize(ref scratchWriter, kvp.Value);
                var valEnd = scratchWriter.WrittenCount;

                pairs[i++] = new PairOffsets(keyStart, keyEnd - keyStart, keyEnd, valEnd - keyEnd);
            }

            scratchWriter.Flush();

            // Sort by serialized key bytes. The comparer is a struct that holds a
            // ReadOnlyMemory<byte> view of the scratch — passed by value through the
            // generic constraint, no boxing.
            pairs.Sort(new ByteRangeComparer(scratch.WrittenMemory));

            var buffer = scratch.WrittenSpan;

            // Keys that differ by Equals can still share their bytes, and the sort puts any such keys side by side.
            // Checking here, before the emit loop, keeps a rejected map out of the real writer.
            for (var j = 1; j < pairs.Length; j++)
            {
                var previous = buffer.Slice(pairs[j - 1].KeyOffset, pairs[j - 1].KeyLen);
                var current = buffer.Slice(pairs[j].KeyOffset, pairs[j].KeyLen);

                if (current.SequenceEqual(previous))
                {
                    ThrowHelper.ThrowDuplicateMapKey();
                }
            }

            // Emit in sorted order into the real writer.
            foreach (var pair in pairs)
            {
                writer.WriteBytes(buffer.Slice(pair.KeyOffset, pair.KeyLen));
                writer.WriteBytes(buffer.Slice(pair.ValOffset, pair.ValLen));
            }
        }
        finally
        {
            scratch.Return();
            if (rentedPairs is not null)
            {
                ArrayPool<PairOffsets>.Shared.Return(rentedPairs);
            }
        }
    }

    public Dictionary<TKey, TValue> Deserialize(ref BcsReader reader)
    {
        Dictionary<TKey, TValue>? result = null;
        Deserialize(ref reader, ref result!);
        return result!;
    }

    public void Deserialize(ref BcsReader reader, ref Dictionary<TKey, TValue> value)
    {
        var count = reader.ReadLength();
        var capacity = InitialCapacity(count, ref reader);

        if (value is null)
        {
            value = new Dictionary<TKey, TValue>(capacity);
        }
        else
        {
            value.Clear();
            value.EnsureCapacity(capacity);
        }

        if (count == 0)
        {
            return;
        }

        var prevKeyStart = -1;
        var prevKeyEnd = -1;

        for (var i = 0; i < count; i++)
        {
            var keyStart = reader.Position;
            var key = _keyFormatter.Deserialize(ref reader);
            var keyEnd = reader.Position;

            var val = _valueFormatter.Deserialize(ref reader);

            if (prevKeyStart >= 0)
            {
                var source = reader.Source;
                var prevKey = source.Slice(prevKeyStart, prevKeyEnd - prevKeyStart);
                var currKey = source.Slice(keyStart, keyEnd - keyStart);
                if (currKey.SequenceCompareTo(prevKey) <= 0)
                {
                    throw new InvalidOperationException("Map keys must be in strictly increasing lexicographical order by BCS bytes");
                }
            }

            prevKeyStart = keyStart;
            prevKeyEnd = keyEnd;

            if (!value.TryAdd(key, val))
            {
                throw new InvalidOperationException($"Duplicate key found in map: {key}");
            }
        }
    }

    /// <summary>
    /// Caps the up-front capacity at what the remaining input could fill, because the count comes from the input and a few bytes can claim billions of entries.
    /// </summary>
    private static int InitialCapacity(int count, ref BcsReader reader) => Math.Min(count, reader.RemainingBytes);

    /// <summary>Byte offsets and lengths into a shared serialization scratch buffer.</summary>
    private readonly struct PairOffsets(int keyOffset, int keyLen, int valOffset, int valLen)
    {
        public readonly int KeyOffset = keyOffset;
        public readonly int KeyLen = keyLen;
        public readonly int ValOffset = valOffset;
        public readonly int ValLen = valLen;
    }

    /// <summary>
    /// Struct comparer used by <see cref="MemoryExtensions.Sort{T, TComparer}(Span{T}, TComparer)"/>.
    /// Holds the buffer as <see cref="ReadOnlyMemory{T}"/> (heap-friendly, can be a struct field) and compares slices via <c>SequenceCompareTo</c>.
    /// </summary>
    private readonly struct ByteRangeComparer(ReadOnlyMemory<byte> buffer) : IComparer<PairOffsets>
    {
        private readonly ReadOnlyMemory<byte> _buffer = buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(PairOffsets x, PairOffsets y)
        {
            var span = _buffer.Span;
            return span.Slice(x.KeyOffset, x.KeyLen)
                .SequenceCompareTo(span.Slice(y.KeyOffset, y.KeyLen));
        }
    }
}
