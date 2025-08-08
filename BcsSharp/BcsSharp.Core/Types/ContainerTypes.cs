using System;
using System.Collections.Generic;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// BCS type for vectors (arrays) of other types
    /// </summary>
    public class VectorType<T> : BcsType<T[]>
    {
        private readonly BcsType<T> _elementType;

        public VectorType(BcsType<T> elementType) : base($"vector<{elementType.Name}>")
        {
            _elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }

        public override T[] Read(BcsReader reader)
        {
            var length = reader.ReadULEB32();
            var result = new T[length];

            for (uint i = 0; i < length; i++)
            {
                result[i] = _elementType.Read(reader);
            }

            return result;
        }

        public override void Write(T[] value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            writer.WriteULEB((uint)value.Length);

            foreach (var item in value)
            {
                _elementType.Write(item, writer);
            }
        }

        public override int? SerializedSize(T[] value)
        {
            if (value == null) return null;

            // Try to calculate total size
            int totalSize = 0;

            // Add ULEB size (simplified - actual ULEB size varies)
            if (value.Length < 128) totalSize += 1;
            else return null; // Complex ULEB calculation

            foreach (var item in value)
            {
                var itemSize = _elementType.SerializedSize(item);
                if (itemSize == null) return null;
                totalSize += itemSize.Value;
            }

            return totalSize;
        }
    }

    /// <summary>
    /// BCS type for optional values - works with both value and reference types
    /// </summary>
    public class OptionType<T> : BcsType<T?>
    {
        private readonly BcsType<T> _innerType;

        public OptionType(BcsType<T> innerType) : base($"option<{innerType.Name}>")
        {
            _innerType = innerType ?? throw new ArgumentNullException(nameof(innerType));
        }

        public override T? Read(BcsReader reader)
        {
            var hasValue = reader.ReadBool();
            return hasValue ? _innerType.Read(reader) : default(T?);
        }

        public override void Write(T? value, BcsWriter writer)
        {
            if (EqualityComparer<T?>.Default.Equals(value, default(T?)))
            {
                writer.WriteBool(false);
            }
            else
            {
                writer.WriteBool(true);
                _innerType.Write(value!, writer);
            }
        }

        public override int? SerializedSize(T? value)
        {
            if (EqualityComparer<T?>.Default.Equals(value, default(T?)))
                return 1; // Just the bool flag

            var innerSize = _innerType.SerializedSize(value!);
            return innerSize == null ? null : 1 + innerSize.Value;
        }
    }
}