using System.Runtime.CompilerServices;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// Formatter for Dictionary/Map types in BCS format
/// Keys are sorted by lexicographical order of their BCS serialized bytes for deterministic output
/// </summary>
public sealed class MapFormatter<TKey, TValue> : IBcsFormatter<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    private readonly IBcsFormatter<TKey> _keyFormatter;
    private readonly IBcsFormatter<TValue> _valueFormatter;

    private static readonly BcsWriterOptions _internalWriterOptions = new()
    {
        InitialBufferSize = 256
    };

    public Type TargetType => typeof(Dictionary<TKey, TValue>);

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

        writer.WriteULEB((uint)value.Count);

        // Serialize key-value pairs and sort by lexicographical order of key bytes (BCS requirement)
        var serializedPairs = new List<(byte[] keyBytes, byte[] valueBytes)>(value.Count);
        var tempWriter = new BcsWriter(_internalWriterOptions);

        foreach (var kvp in value)
        {
            // Serialize key
            tempWriter.Reset();
            _keyFormatter.Serialize(ref tempWriter, kvp.Key);
            var keyBytes = tempWriter.ToBytes();

            // Serialize value
            tempWriter.Reset();
            _valueFormatter.Serialize(ref tempWriter, kvp.Value);
            var valueBytes = tempWriter.ToBytes();

            serializedPairs.Add((keyBytes, valueBytes));
        }

        // Sort by lexicographical order of serialized key bytes
        serializedPairs.Sort((a, b) => CompareByteArrays(a.keyBytes, b.keyBytes));

        // Write sorted key-value pairs  
        foreach (var pair in serializedPairs)
        {
            writer.WriteBytes(pair.keyBytes);
            writer.WriteBytes(pair.valueBytes);
        }
    }

    public Dictionary<TKey, TValue> Deserialize(ref BcsReader reader)
    {
        var count = reader.ReadULEB32();
        if (count == 0)
            return new Dictionary<TKey, TValue>();

        var result = new Dictionary<TKey, TValue>((int)count);
        byte[]? previousKeyBytes = null;

        for (uint i = 0; i < count; i++)
        {
            var key = _keyFormatter.Deserialize(ref reader);
            var value = _valueFormatter.Deserialize(ref reader);

            // Verify keys are in sorted order (BCS requirement)
            if (i > 0 && previousKeyBytes != null)
            {
                var keyWriter = new BcsWriter();
                _keyFormatter.Serialize(ref keyWriter, key);
                var currentKeyBytes = keyWriter.ToBytes();

                if (CompareByteArrays(currentKeyBytes, previousKeyBytes) <= 0)
                {
                    throw new InvalidOperationException("Map keys must be in strictly increasing lexicographical order by BCS bytes");
                }

                previousKeyBytes = currentKeyBytes;
            }
            else if (i == 0)
            {
                var keyWriter = new BcsWriter();
                _keyFormatter.Serialize(ref keyWriter, key);
                previousKeyBytes = keyWriter.ToBytes();
            }

            if (result.ContainsKey(key))
            {
                throw new InvalidOperationException($"Duplicate key found in map: {key}");
            }

            result.Add(key, value);
        }

        return result;
    }


    private static int GetULEBSize(uint value)
    {
        if (value < 0x80) return 1;
        if (value < 0x4000) return 2;
        if (value < 0x200000) return 3;
        if (value < 0x10000000) return 4;
        return 5;
    }

    /// <summary>
    /// Compares two byte arrays lexicographically
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CompareByteArrays(byte[] a, byte[] b)
    {
        return a.AsSpan().SequenceCompareTo(b.AsSpan());
    }
}
