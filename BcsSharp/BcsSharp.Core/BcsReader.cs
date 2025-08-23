using BcsSharp.Core.Helpers;
using Nethermind.Int256;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BcsSharp.Core
{
    /// <summary>
    /// Class used for reading BCS data chunk by chunk.
    /// </summary>
    public ref struct BcsReader
    {
        private readonly ReadOnlyMemory<byte> _data;
        private int _position;

        public BcsReader(byte[] data)
        {
            _data = data;
            _position = 0;
        }

        public BcsReader(ReadOnlyMemory<byte> data)
        {
            _data = data;
            _position = 0;
        }

        /// <summary>
        /// Get remaining bytes count
        /// </summary>
        public int RemainingBytes => _data.Length - _position;

        /// <summary>
        /// Check if there are remaining bytes to read
        /// </summary>
        public bool HasRemainingBytes => _position < _data.Length;

        /// <summary>
        /// Read a single byte (u8)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Read8()
        {
            EnsureEnoughBytes(1);

            return _data.Span[_position++];
        }

        /// <summary>
        /// Read 16-bit unsigned integer (u16) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Read16()
        {
            EnsureEnoughBytes(2);

            var result = BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(_position, 2));
            _position += 2;
            return result;
        }

        /// <summary>
        /// Read 32-bit unsigned integer (u32) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Read32()
        {
            EnsureEnoughBytes(4);
            var result = BinaryPrimitives.ReadUInt32LittleEndian(_data.Span.Slice(_position, 4));
            _position += 4;
            return result;
        }

        /// <summary>
        /// Read 64-bit unsigned integer (u64) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Read64()
        {
            EnsureEnoughBytes(8);
            var result = BinaryPrimitives.ReadUInt64LittleEndian(_data.Span.Slice(_position, 8));
            _position += 8;
            return result;
        }

        /// <summary>
        /// Read signed 8-bit integer (i8)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadI8()
        {
            EnsureEnoughBytes(1);

            return (sbyte)_data.Span[_position++];
        }

        /// <summary>
        /// Read signed 16-bit integer (i16) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadI16()
        {
            EnsureEnoughBytes(2);

            var result = BinaryPrimitives.ReadInt16LittleEndian(_data.Span.Slice(_position, 2));
            _position += 2;
            return result;
        }

        /// <summary>
        /// Read signed 32-bit integer (i32) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadI32()
        {
            EnsureEnoughBytes(4);
            var result = BinaryPrimitives.ReadInt32LittleEndian(_data.Span.Slice(_position, 4));
            _position += 4;
            return result;
        }

        /// <summary>
        /// Read signed 64-bit integer (i64) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadI64()
        {
            EnsureEnoughBytes(8);
            var result = BinaryPrimitives.ReadInt64LittleEndian(_data.Span.Slice(_position, 8));
            _position += 8;
            return result;
        }

        /// <summary>
        /// Read signed 128-bit integer (i128)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int128 ReadI128()
        {
            EnsureEnoughBytes(16);
            var span = _data.Span.Slice(_position, 16);
            _position += 16;

            // Convert from little-endian bytes to Int128
            return BinaryPrimitives.ReadInt128LittleEndian(span);
        }

        /// <summary>
        /// Read 128-bit unsigned integer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt128 Read128()
        {
            EnsureEnoughBytes(16);
            var span = _data.Span.Slice(_position, 16);
            _position += 16;

            // Convert from little-endian bytes to UInt128
            return BinaryPrimitives.ReadUInt128LittleEndian(span);
        }

        /// <summary>
        /// Read 256-bit unsigned integer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256 Read256()
        {
            EnsureEnoughBytes(32);
            var span = _data.Span.Slice(_position, 32);
            _position += 32;

            // Create UInt256 from little-endian bytes - false means little-endian
            return new UInt256(span, isBigEndian: false);
        }

        /// <summary>
        /// Read specified number of bytes
        /// </summary>
        public byte[] ReadBytes(int length)
        {
            if (length < 0)
                throw new ArgumentException("Length cannot be negative", nameof(length));

            EnsureEnoughBytes(length);

            var result = _data.Span.Slice(_position, length).ToArray();
            _position += length;
            return result;
        }

        /// <summary>
        /// Read specified number of bytes as ReadOnlySpan
        /// </summary>
        public ReadOnlySpan<byte> ReadBytesAsSpan(int length)
        {
            if (length < 0)
                throw new ArgumentException("Length cannot be negative", nameof(length));

            EnsureEnoughBytes(length);

            var result = _data.Span.Slice(_position, length);
            _position += length;
            return result;
        }

        /// <summary>
        /// Read ULEB128 as 32-bit uint (throws if value is too large)
        /// </summary>
        public uint ReadULEB32()
        {
            uint result = 0;
            ReadOnlySpan<byte> span = _data.Span;
            int startPosition = _position;

            // Byte 1
            byte b = span[_position++];
            result = (uint)(b & 0x7F);
            if ((b & 0x80) == 0)
                return result;

            // Byte 2
            b = span[_position++];
            result |= (uint)(b & 0x7F) << 7;
            if ((b & 0x80) == 0)
            {
                // Value encoded in 2 bytes - check if it could fit in 1 byte
                if (result < 0x80)  // Values 0-127 should be encoded in 1 byte
                {
                    _position = startPosition;
                    ThrowHelper.ThrowInvalidOperationException("Non-canonical ULEB128 encoding detected");
                }
                return result;
            }

            // Byte 3
            b = span[_position++];
            result |= (uint)(b & 0x7F) << 14;
            if ((b & 0x80) == 0)
            {
                // Value encoded in 3 bytes - check if it could fit in 2 bytes
                if (result < 0x4000)  // Values 0-16383 should be encoded in max 2 bytes
                {
                    _position = startPosition;
                    ThrowHelper.ThrowInvalidOperationException("Non-canonical ULEB128 encoding detected");
                }
                return result;
            }

            // Byte 4
            b = span[_position++];
            result |= (uint)(b & 0x7F) << 21;
            if ((b & 0x80) == 0)
            {
                // Value encoded in 4 bytes - check if it could fit in 3 bytes
                if (result < 0x200000)  // Values 0-2097151 should be encoded in max 3 bytes
                {
                    _position = startPosition;
                    ThrowHelper.ThrowInvalidOperationException("Non-canonical ULEB128 encoding detected");
                }
                return result;
            }

            // Byte 5
            b = span[_position++];
            result |= (uint)(b & 0x7F) << 28;
            if ((b & 0x80) != 0) // last bit indicating there are more bytes for ULEB128
            {
                ThrowHelper.ThrowInvalidOperationException("ULEB128 encoding too long for uint");
            }

            // Value encoded in 5 bytes - check if it could fit in 4 bytes
            if (result < 0x10000000)  // Values 0-268435455 should be encoded in max 4 bytes
            {
                _position = startPosition;
                ThrowHelper.ThrowInvalidOperationException("Non-canonical ULEB128 encoding detected");
            }

            return result;
        }

        /// <summary>
        /// Read boolean value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            var value = Read8();
            if (value > 1) // corrupted or invalid boolean value
            {
                ThrowInvalidOperationException($"Invalid boolean value: {value}");
            }

            return value == 1;

            void ThrowInvalidOperationException(string message)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Read string value (length-prefixed UTF-8)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString()
        {
            var length = ReadULEB32();
            var span = ReadBytesAsSpan(unchecked((int)length));
            return System.Text.Encoding.UTF8.GetString(span);
        }

        /// <summary>
        /// Read primitive array using vectorized operations for maximum performance.
        /// Uses MemoryMarshal to directly copy memory without element-by-element deserialization.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadPrimitiveArray<T>(Span<T> destination) where T : unmanaged
        {
            var byteLength = destination.Length * Unsafe.SizeOf<T>();
            EnsureEnoughBytes(byteLength);

            var sourceBytes = _data.Span.Slice(_position, byteLength);
            var destBytes = MemoryMarshal.AsBytes(destination);
            sourceBytes.CopyTo(destBytes);
            _position += byteLength;
        }

        /// <summary>
        /// Get current position
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// Reset position to beginning
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureEnoughBytes(int count)
        {
            if (_position + count > _data.Length)
            {
                ThrowHelper.ThrowEndOfStreamException(count);
            }
        }
    }
}