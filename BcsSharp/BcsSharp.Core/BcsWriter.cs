using Nethermind.Int256;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;

namespace BcsSharp.Core
{
    /// <summary>
    /// Options for BcsWriter initialization
    /// </summary>
    public class BcsWriterOptions
    {
        public int InitialSize { get; set; } = 1024;
    }

    /// <summary>
    /// Class used to write BCS data into a buffer.
    /// Most methods are chainable for fluent usage.
    /// </summary>
    public class BcsWriter
    {
        private readonly IBufferWriter<byte> _bufferWriter;
        private readonly ArrayBufferWriter<byte>? _defaultBuffer;
        private readonly bool _ownsBuffer;

        public BcsWriter(BcsWriterOptions? options = null)
        {
            options ??= new BcsWriterOptions();
            _defaultBuffer = new ArrayBufferWriter<byte>(options.InitialSize);
            _bufferWriter = _defaultBuffer;
            _ownsBuffer = true;
        }

        public BcsWriter(IBufferWriter<byte> bufferWriter)
        {
            _bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            _defaultBuffer = null;
            _ownsBuffer = false;
        }

        /// <summary>
        /// Get current length of written data
        /// </summary>
        public int Length => _ownsBuffer ? _defaultBuffer!.WrittenCount :
            throw new InvalidOperationException("Length is not available when using external IBufferWriter");

        /// <summary>
        /// Write bytes to the buffer writer
        /// </summary>
        private void WriteToBuffer(ReadOnlySpan<byte> data)
        {
            var span = _bufferWriter.GetSpan(data.Length);
            data.CopyTo(span);
            _bufferWriter.Advance(data.Length);
        }

        /// <summary>
        /// Write a single byte (u8)
        /// </summary>
        public BcsWriter Write8(byte value)
        {
            var span = _bufferWriter.GetSpan(1);
            span[0] = value;
            _bufferWriter.Advance(1);
            return this;
        }

        /// <summary>
        /// Write 16-bit unsigned integer (u16) in little-endian format
        /// </summary>
        public BcsWriter Write16(ushort value)
        {
            var span = _bufferWriter.GetSpan(2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
            _bufferWriter.Advance(2);
            return this;
        }

        /// <summary>
        /// Write 32-bit unsigned integer (u32) in little-endian format
        /// </summary>
        public BcsWriter Write32(uint value)
        {
            var span = _bufferWriter.GetSpan(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
            _bufferWriter.Advance(4);
            return this;
        }

        /// <summary>
        /// Write 64-bit unsigned integer (u64) in little-endian format
        /// </summary>
        public BcsWriter Write64(ulong value)
        {
            var span = _bufferWriter.GetSpan(8);
            BinaryPrimitives.WriteUInt64LittleEndian(span, value);
            _bufferWriter.Advance(8);
            return this;
        }

        /// <summary>
        /// Write signed 8-bit integer (i8)
        /// </summary>
        public BcsWriter WriteI8(sbyte value)
        {
            var span = _bufferWriter.GetSpan(1);
            span[0] = (byte)value;
            _bufferWriter.Advance(1);
            return this;
        }

        /// <summary>
        /// Write signed 16-bit integer (i16) in little-endian format
        /// </summary>
        public BcsWriter WriteI16(short value)
        {
            var span = _bufferWriter.GetSpan(2);
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
            _bufferWriter.Advance(2);
            return this;
        }

        /// <summary>
        /// Write signed 32-bit integer (i32) in little-endian format
        /// </summary>
        public BcsWriter WriteI32(int value)
        {
            var span = _bufferWriter.GetSpan(4);
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            _bufferWriter.Advance(4);
            return this;
        }

        /// <summary>
        /// Write signed 64-bit integer (i64) in little-endian format
        /// </summary>
        public BcsWriter WriteI64(long value)
        {
            var span = _bufferWriter.GetSpan(8);
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
            _bufferWriter.Advance(8);
            return this;
        }

        /// <summary>
        /// Write signed 128-bit integer (i128) in little-endian format
        /// </summary>
        public BcsWriter WriteI128(Int128 value)
        {
            var span = _bufferWriter.GetSpan(16);
            BinaryPrimitives.WriteInt128LittleEndian(span, value);
            _bufferWriter.Advance(16);
            return this;
        }

        /// <summary>
        /// Write 128-bit unsigned integer in little-endian format
        /// </summary>
        public BcsWriter Write128(UInt128 value)
        {
            var span = _bufferWriter.GetSpan(16);
            BinaryPrimitives.WriteUInt128LittleEndian(span, value);
            _bufferWriter.Advance(16);
            return this;
        }

        /// <summary>
        /// Write 256-bit unsigned integer
        /// </summary>
        public BcsWriter Write256(UInt256 value)
        {
            // we must do this instead of direct buffer write because there is internal check for buffer size
            Span<byte> littleEndianBytes = stackalloc byte[32];
            value.ToLittleEndian(littleEndianBytes);

            var span = _bufferWriter.GetSpan(32);
            littleEndianBytes.CopyTo(span);
            _bufferWriter.Advance(32);
            return this;
        }

        /// <summary>
        /// Write raw bytes
        /// </summary>
        public BcsWriter WriteBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            WriteToBuffer(bytes);
            return this;
        }

        /// <summary>
        /// Write raw bytes from span
        /// </summary>
        public BcsWriter WriteBytes(ReadOnlySpan<byte> bytes)
        {
            WriteToBuffer(bytes);
            return this;
        }

        /// <summary>
        /// Write ULEB128 (Variable Length Encoding) integer
        /// </summary>
        public BcsWriter WriteULEB(UInt128 value)
        {
            Span<byte> tempBuffer = stackalloc byte[19]; // Max bytes for 128-bit ULEB128
            int bytesWritten = 0;

            do
            {
                byte b = (byte)(value & 0x7F);
                value >>= 7;

                if (value != 0)
                    b |= 0x80;

                tempBuffer[bytesWritten++] = b;
            } while (value != 0);

            WriteToBuffer(tempBuffer[..bytesWritten]);
            return this;
        }

        /// <summary>
        /// Write ULEB128 from 32-bit uint
        /// </summary>
        public BcsWriter WriteULEB(uint value)
        {
            return WriteULEB(new UInt128(0, value));
        }

        /// <summary>
        /// Write ULEB128 from 64-bit ulong
        /// </summary>
        public BcsWriter WriteULEB(ulong value)
        {
            return WriteULEB(new UInt128(0, value));
        }

        /// <summary>
        /// Write boolean value
        /// </summary>
        public BcsWriter WriteBool(bool value)
        {
            return Write8(Convert.ToByte(value));
        }

        /// <summary>
        /// Write string value (length-prefixed UTF-8)
        /// </summary>
        public BcsWriter WriteString(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteULEB((uint)bytes.Length);
            WriteBytes(bytes);
            return this;
        }

        /// <summary>
        /// Get the written data as byte array
        /// </summary>
        public byte[] ToBytes()
        {
            if (!_ownsBuffer)
                throw new InvalidOperationException("ToBytes is not available when using external IBufferWriter");
            return _defaultBuffer!.WrittenSpan.ToArray();
        }

        /// <summary>
        /// Get the written data as hex string
        /// </summary>
        public string ToHex()
        {
            if (!_ownsBuffer)
                throw new InvalidOperationException("ToHex is not available when using external IBufferWriter");
            return Convert.ToHexString(_defaultBuffer!.WrittenSpan).ToLowerInvariant();
        }

        /// <summary>
        /// Get the written data as base64 string
        /// </summary>
        public string ToBase64()
        {
            if (!_ownsBuffer)
                throw new InvalidOperationException("ToBase64 is not available when using external IBufferWriter");
            return Convert.ToBase64String(_defaultBuffer!.WrittenSpan);
        }

        /// <summary>
        /// Reset the writer to empty state
        /// </summary>
        public BcsWriter Reset()
        {
            if (!_ownsBuffer)
                throw new InvalidOperationException("Reset is not available when using external IBufferWriter");
            _defaultBuffer!.Clear();
            return this;
        }

        /// <summary>
        /// Get current position (same as Length)
        /// </summary>
        public int Position => Length;
    }
}