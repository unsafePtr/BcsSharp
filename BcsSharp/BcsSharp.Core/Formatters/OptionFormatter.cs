using System;
using System.Reflection;

namespace BcsSharp.Core.Formatters
{
    public static class OptionFormatterCache
    {
        public static readonly OptionFormatter<byte> ByteOptionFormatter = new OptionFormatter<byte>(ByteFormatter.Instance);
        public static readonly OptionFormatter<sbyte> SByteOptionFormatter = new OptionFormatter<sbyte>(SByteFormatter.Instance);
        public static readonly OptionFormatter<ushort> UInt16OptionFormatter = new OptionFormatter<ushort>(UInt16Formatter.Instance);
        public static readonly OptionFormatter<short> Int16OptionFormatter = new OptionFormatter<short>(Int16Formatter.Instance);
        public static readonly OptionFormatter<uint> UInt32OptionFormatter = new OptionFormatter<uint>(UInt32Formatter.Instance);
        public static readonly OptionFormatter<int> Int32OptionFormatter = new OptionFormatter<int>(Int32Formatter.Instance);
        public static readonly OptionFormatter<ulong> UInt64OptionFormatter = new OptionFormatter<ulong>(UInt64Formatter.Instance);
        public static readonly OptionFormatter<long> Int64OptionFormatter = new OptionFormatter<long>(Int64Formatter.Instance);
        public static readonly OptionFormatter<UInt128> UInt128OptionFormatter = new OptionFormatter<UInt128>(UInt128Formatter.Instance);
        public static readonly OptionFormatter<Int128> Int128OptionFormatter = new OptionFormatter<Int128>(Int128Formatter.Instance);
        public static readonly OptionFormatter<Nethermind.Int256.UInt256> UInt256OptionFormatter = new OptionFormatter<Nethermind.Int256.UInt256>(UInt256Formatter.Instance);
        public static readonly OptionFormatter<bool> BoolOptionFormatter = new OptionFormatter<bool>(BoolFormatter.Instance);
    }

    /// <summary>
    /// Formatter for BCS Option<T> type - handles nullable value types
    /// </summary>
    public sealed class OptionFormatter<T> : IBcsFormatter<T?> where T : struct
    {
        private readonly IBcsFormatter<T> _valueFormatter;

        public Type TargetType => typeof(T?);

        public OptionFormatter(IBcsFormatter<T> valueFormatter)
        {
            _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
        }

        public void Serialize(ref BcsWriter writer, T? value)
        {
            if (value.HasValue)
            {
                // Write 1 to indicate Some(value)
                writer.Write((byte)1);
                _valueFormatter.Serialize(ref writer, value.Value);
            }
            else
            {
                // Write 0 to indicate None
                writer.Write((byte)0);
            }
        }

        public T? Deserialize(ref BcsReader reader)
        {
            var hasValue = reader.Read8();
            if (hasValue == 0)
            {
                return null;
            }
            else if (hasValue == 1)
            {
                return _valueFormatter.Deserialize(ref reader);
            }
            else
            {
                throw new InvalidOperationException($"Invalid Option discriminant: {hasValue}. Expected 0 (None) or 1 (Some).");
            }
        }

        public int? GetSerializedSize(T? value)
        {
            if (!value.HasValue)
            {
                return 1; // Just the discriminant byte
            }

            var valueSize = _valueFormatter.GetSerializedSize(value.Value);
            if (valueSize == null)
                return null; // Variable size

            return 1 + valueSize.Value; // Discriminant + value
        }
    }

    /// <summary>
    /// Formatter for BCS Option<T> type - handles nullable reference types
    /// </summary>
    public sealed class NullableReferenceFormatter<T> : IBcsFormatter<T?> where T : class
    {
        private readonly IBcsFormatter<T> _valueFormatter;

        public Type TargetType => typeof(T);

        public NullableReferenceFormatter(IBcsFormatter<T> valueFormatter)
        {
            _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
        }

        public void Serialize(ref BcsWriter writer, T? value)
        {
            if (value != null)
            {
                // Write 1 to indicate Some(value)
                writer.Write((byte)1);
                _valueFormatter.Serialize(ref writer, value);
            }
            else
            {
                // Write 0 to indicate None
                writer.Write((byte)0);
            }
        }

        public T? Deserialize(ref BcsReader reader)
        {
            var hasValue = reader.Read8();
            if (hasValue == 0)
            {
                return null;
            }
            else if (hasValue == 1)
            {
                return _valueFormatter.Deserialize(ref reader);
            }
            else
            {
                throw new InvalidOperationException($"Invalid Option discriminant: {hasValue}. Expected 0 (None) or 1 (Some).");
            }
        }

        public int? GetSerializedSize(T? value)
        {
            if (value == null)
            {
                return 1; // Just the discriminant byte
            }

            var valueSize = _valueFormatter.GetSerializedSize(value);
            if (valueSize == null)
                return null; // Variable size

            return 1 + valueSize.Value; // Discriminant + value
        }
    }

    /// <summary>
    /// Static helper to create Option formatters
    /// </summary>
    public static class OptionFormatterHelper
    {
        /// <summary>
        /// Create formatter for nullable value type
        /// </summary>
        public static OptionFormatter<T> ForValueType<T>(IBcsFormatter<T> valueFormatter) where T : struct
        {
            return new OptionFormatter<T>(valueFormatter);
        }

        /// <summary>
        /// Create formatter for nullable reference type
        /// </summary>
        public static NullableReferenceFormatter<T> ForReferenceType<T>(IBcsFormatter<T> valueFormatter) where T : class
        {
            return new NullableReferenceFormatter<T>(valueFormatter);
        }

        /// <summary>
        /// Check if a type is a nullable reference type using reflection
        /// Note: This is a simplified check. For full nullable reference type detection,
        /// more complex analysis of NullableAttribute and NullableContextAttribute is needed.
        /// </summary>
        public static bool IsNullableReferenceType(Type type)
        {
            // Check if it's a reference type
            if (type.IsValueType)
                return false;

            // For generic nullable value types (T?)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return false;

            // For simplicity, assume reference types can be nullable
            // In a real implementation, you'd need to check NullableAttribute and NullableContextAttribute
            return true;
        }
    }
}