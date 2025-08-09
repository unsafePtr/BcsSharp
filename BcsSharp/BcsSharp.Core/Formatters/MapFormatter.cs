using System;
using System.Collections.Generic;
using System.Linq;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// Formatter for Dictionary/Map types in BCS format
    /// Keys are serialized in sorted order for deterministic output
    /// </summary>
    public sealed class MapFormatter<TKey, TValue> : IBcsFormatter<Dictionary<TKey, TValue>>
        where TKey : IComparable<TKey>
    {
        private readonly IBcsFormatter<TKey> _keyFormatter;
        private readonly IBcsFormatter<TValue> _valueFormatter;
        
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
            
            // Sort keys for deterministic serialization
            var sortedPairs = value.OrderBy(kvp => kvp.Key).ToList();
            
            foreach (var kvp in sortedPairs)
            {
                _keyFormatter.Serialize(ref writer, kvp.Key);
                _valueFormatter.Serialize(ref writer, kvp.Value);
            }
        }
        
        public Dictionary<TKey, TValue> Deserialize(ref BcsReader reader)
        {
            var count = reader.ReadULEB32();
            if (count == 0)
                return new Dictionary<TKey, TValue>();
            
            var result = new Dictionary<TKey, TValue>((int)count);
            TKey? previousKey = default;
            
            for (uint i = 0; i < count; i++)
            {
                var key = _keyFormatter.Deserialize(ref reader);
                var value = _valueFormatter.Deserialize(ref reader);
                
                // Verify keys are in sorted order (BCS requirement)
                if (i > 0 && previousKey != null && key.CompareTo(previousKey) <= 0)
                {
                    throw new InvalidOperationException("Map keys must be in strictly increasing order");
                }
                
                if (result.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate key found in map: {key}");
                }
                
                result.Add(key, value);
                previousKey = key;
            }
            
            return result;
        }
        
        public int? GetSerializedSize(Dictionary<TKey, TValue> value)
        {
            if (value == null)
                return GetULEBSize(0);
            
            var size = GetULEBSize((uint)value.Count);
            
            foreach (var kvp in value)
            {
                var keySize = _keyFormatter.GetSerializedSize(kvp.Key);
                var valueSize = _valueFormatter.GetSerializedSize(kvp.Value);
                
                if (keySize == null || valueSize == null)
                    return null; // Variable size
                    
                size += keySize.Value + valueSize.Value;
            }
            
            return size;
        }
        
        private static int GetULEBSize(uint value)
        {
            if (value < 0x80) return 1;
            if (value < 0x4000) return 2;
            if (value < 0x200000) return 3;
            if (value < 0x10000000) return 4;
            return 5;
        }
    }
}