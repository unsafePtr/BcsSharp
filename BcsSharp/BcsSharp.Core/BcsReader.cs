using System;
using System.Buffers.Binary;
using System.Text;
using Nethermind.Int256;

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
        /// Shift current cursor position by specified number of bytes
        /// </summary>
        public BcsReader Shift(int bytes)
        {
            if (_position + bytes > _data.Length)
                throw new InvalidOperationException("Not enough bytes to shift");

            _position += bytes;
            return this;
        }

        /// <summary>
        /// Read a single byte (u8)
        /// </summary>
        public byte Read8()
        {
            if (_position >= _data.Length)
                throw new InvalidOperationException("Not enough bytes to read u8");

            return _data.Span[_position++];
        }

        /// <summary>
        /// Read 16-bit unsigned integer (u16) in little-endian format
        /// </summary>
        public ushort Read16()
        {
            if (_position + 2 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read u16");

            var result = BinaryPrimitives.ReadUInt16LittleEndian(_data.Span.Slice(_position, 2));
            _position += 2;
            return result;
        }

        /// <summary>
        /// Read 32-bit unsigned integer (u32) in little-endian format
        /// </summary>
        public uint Read32()
        {
            if (_position + 4 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read u32");

            var result = BinaryPrimitives.ReadUInt32LittleEndian(_data.Span.Slice(_position, 4));
            _position += 4;
            return result;
        }

        /// <summary>
        /// Read 64-bit unsigned integer (u64) in little-endian format
        /// </summary>
        public ulong Read64()
        {
            if (_position + 8 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read u64");

            var result = BinaryPrimitives.ReadUInt64LittleEndian(_data.Span.Slice(_position, 8));
            _position += 8;
            return result;
        }

        /// <summary>
        /// Read signed 8-bit integer (i8)
        /// </summary>
        public sbyte ReadI8()
        {
            if (_position >= _data.Length)
                throw new InvalidOperationException("Not enough bytes to read i8");

            return (sbyte)_data.Span[_position++];
        }

        /// <summary>
        /// Read signed 16-bit integer (i16) in little-endian format
        /// </summary>
        public short ReadI16()
        {
            if (_position + 2 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read i16");

            var result = BinaryPrimitives.ReadInt16LittleEndian(_data.Span.Slice(_position, 2));
            _position += 2;
            return result;
        }

        /// <summary>
        /// Read signed 32-bit integer (i32) in little-endian format
        /// </summary>
        public int ReadI32()
        {
            if (_position + 4 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read i32");

            var result = BinaryPrimitives.ReadInt32LittleEndian(_data.Span.Slice(_position, 4));
            _position += 4;
            return result;
        }

        /// <summary>
        /// Read signed 64-bit integer (i64) in little-endian format
        /// </summary>
        public long ReadI64()
        {
            if (_position + 8 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read i64");

            var result = BinaryPrimitives.ReadInt64LittleEndian(_data.Span.Slice(_position, 8));
            _position += 8;
            return result;
        }

        /// <summary>
        /// Read signed 128-bit integer (i128)
        /// </summary>
        public Int128 ReadI128()
        {
            if (_position + 16 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read i128");

            var span = _data.Span.Slice(_position, 16);
            _position += 16;

            // Convert from little-endian bytes to Int128
            return BinaryPrimitives.ReadInt128LittleEndian(span);
        }

        /// <summary>
        /// Read 128-bit unsigned integer
        /// </summary>
        public UInt128 Read128()
        {
            if (_position + 16 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read u128");

            var span = _data.Span.Slice(_position, 16);
            _position += 16;

            // Convert from little-endian bytes to UInt128
            return BinaryPrimitives.ReadUInt128LittleEndian(span);
        }

        /// <summary>
        /// Read 256-bit unsigned integer
        /// </summary>
        public UInt256 Read256()
        {
            if (_position + 32 > _data.Length)
                throw new InvalidOperationException("Not enough bytes to read u256");

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

            if (_position + length > _data.Length)
                throw new InvalidOperationException($"Not enough bytes to read {length} bytes");

            var result = _data.Span.Slice(_position, length).ToArray();
            _position += length;
            return result;
        }

        /// <summary>
        /// Read specified number of bytes into a span
        /// </summary>
        public void ReadBytes(Span<byte> destination)
        {
            var length = destination.Length;
            if (_position + length > _data.Length)
                throw new InvalidOperationException($"Not enough bytes to read {length} bytes");

            _data.Span.Slice(_position, length).CopyTo(destination);
            _position += length;
        }

        /// <summary>
        /// Read specified number of bytes as ReadOnlySpan
        /// </summary>
        public ReadOnlySpan<byte> ReadBytesAsSpan(int length)
        {
            if (length < 0)
                throw new ArgumentException("Length cannot be negative", nameof(length));

            if (_position + length > _data.Length)
                throw new InvalidOperationException($"Not enough bytes to read {length} bytes");

            var result = _data.Span.Slice(_position, length);
            _position += length;
            return result;
        }

        /// <summary>
        /// Read ULEB128 (Variable Length Encoding) integer - returns UInt128 for 128-bit range
        /// </summary>
        public UInt128 ReadULEB()
        {
            UInt128 result = 0;
            int shift = 0;
            var span = _data.Span;

            while (_position < span.Length)
            {
                byte b = span[_position++];
                result |= new UInt128(0, (uint)(b & 0x7F)) << shift;

                if ((b & 0x80) == 0)
                    break;

                shift += 7;
                if (shift >= 128) // UInt128 can handle up to 127 bits + sign bit
                    throw new InvalidOperationException("ULEB128 value too large for UInt128");
            }

            return result;
        }

        /// <summary>
        /// Read ULEB128 as 32-bit uint (throws if value is too large)
        /// </summary>
        public uint ReadULEB32()
        {
            var value = ReadULEB();
            if (value > uint.MaxValue)
                throw new InvalidOperationException($"ULEB128 value {value} is too large for uint32");
            return (uint)value;
        }

        /// <summary>
        /// Read ULEB128 as 64-bit ulong (throws if value is too large)
        /// </summary>
        public ulong ReadULEB64()
        {
            var value = ReadULEB();
            if (value > ulong.MaxValue)
                throw new InvalidOperationException($"ULEB128 value {value} is too large for uint64");
            return (ulong)value;
        }

        /// <summary>
        /// Read ULEB128 as 128-bit UInt128
        /// </summary>
        public UInt128 ReadULEB128()
        {
            return ReadULEB();
        }

        /// <summary>
        /// Read boolean value
        /// </summary>
        public bool ReadBool()
        {
            var value = Read8();
            if (value > 1)
                throw new InvalidOperationException($"Invalid boolean value: {value}");
            return value == 1;
        }

        /// <summary>
        /// Read string value (length-prefixed UTF-8)
        /// </summary>
        public string ReadString()
        {
            var length = ReadULEB32(); // String length should fit in 32 bits
            var span = ReadBytesAsSpan((int)length);
            return System.Text.Encoding.UTF8.GetString(span);
        }

        /// <summary>
        /// Get current position
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// Reset position to beginning
        /// </summary>
        public void Reset()
        {
            _position = 0;
        }
    }
}