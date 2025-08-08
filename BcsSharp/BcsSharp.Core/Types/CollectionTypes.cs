using System;
using System.Collections.Generic;
using System.Linq;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// BCS type for maps (key-value pairs)
    /// Maps are serialized as vectors of key-value pairs, sorted by key
    /// </summary>
    public class MapType<TKey, TValue> : BcsType<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private readonly BcsType<TKey> _keyType;
        private readonly BcsType<TValue> _valueType;
        private readonly IComparer<TKey> _keyComparer;

        public MapType(BcsType<TKey> keyType, BcsType<TValue> valueType, IComparer<TKey>? keyComparer = null) 
            : base($"map<{keyType.Name}, {valueType.Name}>")
        {
            _keyType = keyType ?? throw new ArgumentNullException(nameof(keyType));
            _valueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            _keyComparer = keyComparer ?? Comparer<TKey>.Default;
        }

        public override Dictionary<TKey, TValue> Read(BcsReader reader)
        {
            var length = reader.ReadULEB32();
            var result = new Dictionary<TKey, TValue>((int)length);

            TKey? previousKey = default;
            bool isFirst = true;

            for (uint i = 0; i < length; i++)
            {
                var key = _keyType.Read(reader);
                var value = _valueType.Read(reader);

                // Verify keys are in sorted order (canonical BCS requirement)
                if (!isFirst && _keyComparer.Compare(previousKey!, key) >= 0)
                {
                    throw new InvalidOperationException("Map keys are not in sorted order");
                }

                if (result.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate key found in map: {key}");
                }

                result[key] = value;
                previousKey = key;
                isFirst = false;
            }

            return result;
        }

        public override void Write(Dictionary<TKey, TValue> value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Sort entries by key for canonical serialization
            var sortedEntries = value.OrderBy(kvp => kvp.Key, _keyComparer).ToList();

            writer.WriteULEB((uint)sortedEntries.Count);

            foreach (var entry in sortedEntries)
            {
                _keyType.Write(entry.Key, writer);
                _valueType.Write(entry.Value, writer);
            }
        }

        public override int? SerializedSize(Dictionary<TKey, TValue> value)
        {
            if (value == null) return null;

            // Calculate ULEB size for length (simplified)
            int totalSize = value.Count < 128 ? 1 : 0; // Simplified ULEB calculation
            if (totalSize == 0) return null; // Complex ULEB, return dynamic

            foreach (var entry in value)
            {
                var keySize = _keyType.SerializedSize(entry.Key);
                var valueSize = _valueType.SerializedSize(entry.Value);
                
                if (keySize == null || valueSize == null) 
                    return null;
                
                totalSize += keySize.Value + valueSize.Value;
            }

            return totalSize;
        }
    }

    /// <summary>
    /// BCS type for sets (unique values)
    /// Sets are serialized as vectors of values, sorted by value
    /// </summary>
    public class SetType<T> : BcsType<HashSet<T>>
        where T : notnull
    {
        private readonly BcsType<T> _elementType;
        private readonly IComparer<T> _comparer;

        public SetType(BcsType<T> elementType, IComparer<T>? comparer = null) 
            : base($"set<{elementType.Name}>")
        {
            _elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public override HashSet<T> Read(BcsReader reader)
        {
            var length = reader.ReadULEB32();
            var result = new HashSet<T>((int)length);

            T? previousValue = default;
            bool isFirst = true;

            for (uint i = 0; i < length; i++)
            {
                var value = _elementType.Read(reader);

                // Verify values are in sorted order (canonical BCS requirement)
                if (!isFirst && _comparer.Compare(previousValue!, value) >= 0)
                {
                    throw new InvalidOperationException("Set values are not in sorted order");
                }

                if (!result.Add(value))
                {
                    throw new InvalidOperationException($"Duplicate value found in set: {value}");
                }

                previousValue = value;
                isFirst = false;
            }

            return result;
        }

        public override void Write(HashSet<T> value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Sort elements for canonical serialization
            var sortedElements = value.OrderBy(x => x, _comparer).ToList();

            writer.WriteULEB((uint)sortedElements.Count);

            foreach (var element in sortedElements)
            {
                _elementType.Write(element, writer);
            }
        }

        public override int? SerializedSize(HashSet<T> value)
        {
            if (value == null) return null;

            // Calculate ULEB size for length (simplified)
            int totalSize = value.Count < 128 ? 1 : 0; // Simplified ULEB calculation
            if (totalSize == 0) return null; // Complex ULEB, return dynamic

            foreach (var element in value)
            {
                var elementSize = _elementType.SerializedSize(element);
                if (elementSize == null) 
                    return null;
                
                totalSize += elementSize.Value;
            }

            return totalSize;
        }
    }

    /// <summary>
    /// BCS type for tuples (fixed-size heterogeneous collections)
    /// </summary>
    public class TupleType<T1, T2> : BcsType<(T1, T2)>
    {
        private readonly BcsType<T1> _type1;
        private readonly BcsType<T2> _type2;

        public TupleType(BcsType<T1> type1, BcsType<T2> type2) 
            : base($"({type1.Name}, {type2.Name})")
        {
            _type1 = type1 ?? throw new ArgumentNullException(nameof(type1));
            _type2 = type2 ?? throw new ArgumentNullException(nameof(type2));
        }

        public override (T1, T2) Read(BcsReader reader)
        {
            var item1 = _type1.Read(reader);
            var item2 = _type2.Read(reader);
            return (item1, item2);
        }

        public override void Write((T1, T2) value, BcsWriter writer)
        {
            _type1.Write(value.Item1, writer);
            _type2.Write(value.Item2, writer);
        }

        public override int? SerializedSize((T1, T2) value)
        {
            var size1 = _type1.SerializedSize(value.Item1);
            var size2 = _type2.SerializedSize(value.Item2);
            
            return (size1 != null && size2 != null) ? size1.Value + size2.Value : null;
        }
    }

    /// <summary>
    /// BCS type for 3-tuples
    /// </summary>
    public class TupleType<T1, T2, T3> : BcsType<(T1, T2, T3)>
    {
        private readonly BcsType<T1> _type1;
        private readonly BcsType<T2> _type2;
        private readonly BcsType<T3> _type3;

        public TupleType(BcsType<T1> type1, BcsType<T2> type2, BcsType<T3> type3) 
            : base($"({type1.Name}, {type2.Name}, {type3.Name})")
        {
            _type1 = type1 ?? throw new ArgumentNullException(nameof(type1));
            _type2 = type2 ?? throw new ArgumentNullException(nameof(type2));
            _type3 = type3 ?? throw new ArgumentNullException(nameof(type3));
        }

        public override (T1, T2, T3) Read(BcsReader reader)
        {
            var item1 = _type1.Read(reader);
            var item2 = _type2.Read(reader);
            var item3 = _type3.Read(reader);
            return (item1, item2, item3);
        }

        public override void Write((T1, T2, T3) value, BcsWriter writer)
        {
            _type1.Write(value.Item1, writer);
            _type2.Write(value.Item2, writer);
            _type3.Write(value.Item3, writer);
        }

        public override int? SerializedSize((T1, T2, T3) value)
        {
            var size1 = _type1.SerializedSize(value.Item1);
            var size2 = _type2.SerializedSize(value.Item2);
            var size3 = _type3.SerializedSize(value.Item3);
            
            return (size1 != null && size2 != null && size3 != null) 
                ? size1.Value + size2.Value + size3.Value 
                : null;
        }
    }

    /// <summary>
    /// BCS type for 4-tuples
    /// </summary>
    public class TupleType<T1, T2, T3, T4> : BcsType<(T1, T2, T3, T4)>
    {
        private readonly BcsType<T1> _type1;
        private readonly BcsType<T2> _type2;
        private readonly BcsType<T3> _type3;
        private readonly BcsType<T4> _type4;

        public TupleType(BcsType<T1> type1, BcsType<T2> type2, BcsType<T3> type3, BcsType<T4> type4) 
            : base($"({type1.Name}, {type2.Name}, {type3.Name}, {type4.Name})")
        {
            _type1 = type1 ?? throw new ArgumentNullException(nameof(type1));
            _type2 = type2 ?? throw new ArgumentNullException(nameof(type2));
            _type3 = type3 ?? throw new ArgumentNullException(nameof(type3));
            _type4 = type4 ?? throw new ArgumentNullException(nameof(type4));
        }

        public override (T1, T2, T3, T4) Read(BcsReader reader)
        {
            var item1 = _type1.Read(reader);
            var item2 = _type2.Read(reader);
            var item3 = _type3.Read(reader);
            var item4 = _type4.Read(reader);
            return (item1, item2, item3, item4);
        }

        public override void Write((T1, T2, T3, T4) value, BcsWriter writer)
        {
            _type1.Write(value.Item1, writer);
            _type2.Write(value.Item2, writer);
            _type3.Write(value.Item3, writer);
            _type4.Write(value.Item4, writer);
        }

        public override int? SerializedSize((T1, T2, T3, T4) value)
        {
            var size1 = _type1.SerializedSize(value.Item1);
            var size2 = _type2.SerializedSize(value.Item2);
            var size3 = _type3.SerializedSize(value.Item3);
            var size4 = _type4.SerializedSize(value.Item4);
            
            return (size1 != null && size2 != null && size3 != null && size4 != null) 
                ? size1.Value + size2.Value + size3.Value + size4.Value 
                : null;
        }
    }

    /// <summary>
    /// BCS type for 5-tuples
    /// </summary>
    public class TupleType<T1, T2, T3, T4, T5> : BcsType<(T1, T2, T3, T4, T5)>
    {
        private readonly BcsType<T1> _type1;
        private readonly BcsType<T2> _type2;
        private readonly BcsType<T3> _type3;
        private readonly BcsType<T4> _type4;
        private readonly BcsType<T5> _type5;

        public TupleType(BcsType<T1> type1, BcsType<T2> type2, BcsType<T3> type3, BcsType<T4> type4, BcsType<T5> type5) 
            : base($"({type1.Name}, {type2.Name}, {type3.Name}, {type4.Name}, {type5.Name})")
        {
            _type1 = type1 ?? throw new ArgumentNullException(nameof(type1));
            _type2 = type2 ?? throw new ArgumentNullException(nameof(type2));
            _type3 = type3 ?? throw new ArgumentNullException(nameof(type3));
            _type4 = type4 ?? throw new ArgumentNullException(nameof(type4));
            _type5 = type5 ?? throw new ArgumentNullException(nameof(type5));
        }

        public override (T1, T2, T3, T4, T5) Read(BcsReader reader)
        {
            var item1 = _type1.Read(reader);
            var item2 = _type2.Read(reader);
            var item3 = _type3.Read(reader);
            var item4 = _type4.Read(reader);
            var item5 = _type5.Read(reader);
            return (item1, item2, item3, item4, item5);
        }

        public override void Write((T1, T2, T3, T4, T5) value, BcsWriter writer)
        {
            _type1.Write(value.Item1, writer);
            _type2.Write(value.Item2, writer);
            _type3.Write(value.Item3, writer);
            _type4.Write(value.Item4, writer);
            _type5.Write(value.Item5, writer);
        }

        public override int? SerializedSize((T1, T2, T3, T4, T5) value)
        {
            var size1 = _type1.SerializedSize(value.Item1);
            var size2 = _type2.SerializedSize(value.Item2);
            var size3 = _type3.SerializedSize(value.Item3);
            var size4 = _type4.SerializedSize(value.Item4);
            var size5 = _type5.SerializedSize(value.Item5);
            
            return (size1 != null && size2 != null && size3 != null && size4 != null && size5 != null) 
                ? size1.Value + size2.Value + size3.Value + size4.Value + size5.Value 
                : null;
        }
    }

    /// <summary>
    /// BCS type for fixed-length arrays
    /// </summary>
    public class FixedArrayType<T> : BcsType<T[]>
    {
        private readonly BcsType<T> _elementType;
        private readonly int _length;

        public FixedArrayType(BcsType<T> elementType, int length) 
            : base($"[{elementType.Name}; {length}]")
        {
            _elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
            _length = length >= 0 ? length : throw new ArgumentException("Length must be non-negative", nameof(length));
        }

        public override T[] Read(BcsReader reader)
        {
            var result = new T[_length];

            for (int i = 0; i < _length; i++)
            {
                result[i] = _elementType.Read(reader);
            }

            return result;
        }

        public override void Write(T[] value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value.Length != _length)
                throw new ArgumentException($"Array length {value.Length} does not match expected length {_length}");

            foreach (var item in value)
            {
                _elementType.Write(item, writer);
            }
        }

        public override int? SerializedSize(T[] value)
        {
            if (value == null) return null;
            if (value.Length != _length) return null;

            int totalSize = 0;
            foreach (var item in value)
            {
                var itemSize = _elementType.SerializedSize(item);
                if (itemSize == null) return null;
                totalSize += itemSize.Value;
            }

            return totalSize;
        }
    }
}