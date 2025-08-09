using Nethermind.Int256;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace BcsSharp.Core
{
    /// <summary>
    /// Options for BcsWriter initialization
    /// </summary>
    public class BcsWriterOptions
    {
        public static readonly BcsWriterOptions Default = new BcsWriterOptions();

        public int InitialSize { get; set; } = 1024;
    }

    /// <summary>
    /// Class used to write BCS data into a buffer.
    /// Most methods are chainable for fluent usage.
    /// </summary>
    public ref struct BcsWriter
    {
        private readonly IBufferWriter<byte> _bufferWriter;
        private readonly ArrayBufferWriter<byte>? _defaultBuffer;
        private readonly bool _ownsBuffer;

        public BcsWriter()
        {
            _defaultBuffer = new ArrayBufferWriter<byte>(BcsWriterOptions.Default.InitialSize);
            _bufferWriter = _defaultBuffer;
            _ownsBuffer = true;
        }

        public BcsWriter(BcsWriterOptions options)
        {
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
        /// Write a single byte (u8)
        /// </summary>
        public void Write(byte value)
        {
            var span = _bufferWriter.GetSpan(1);
            span[0] = value;
            _bufferWriter.Advance(1);

        }

        /// <summary>
        /// Write 16-bit unsigned integer (u16) in little-endian format
        /// </summary>
        public void Write(ushort value)
        {
            var span = _bufferWriter.GetSpan(2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
            _bufferWriter.Advance(2);

        }

        /// <summary>
        /// Write 32-bit unsigned integer (u32) in little-endian format
        /// </summary>
        public void Write(uint value)
        {
            var span = _bufferWriter.GetSpan(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
            _bufferWriter.Advance(4);

        }

        /// <summary>
        /// Write 64-bit unsigned integer (u64) in little-endian format
        /// </summary>
        public void Write(ulong value)
        {
            var span = _bufferWriter.GetSpan(8);
            BinaryPrimitives.WriteUInt64LittleEndian(span, value);
            _bufferWriter.Advance(8);

        }

        /// <summary>
        /// Write signed 8-bit integer (i8)
        /// </summary>
        public void Write(sbyte value)
        {
            var span = _bufferWriter.GetSpan(1);
            span[0] = (byte)value;
            _bufferWriter.Advance(1);

        }

        /// <summary>
        /// Write signed 16-bit integer (i16) in little-endian format
        /// </summary>
        public void Write(short value)
        {
            var span = _bufferWriter.GetSpan(2);
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
            _bufferWriter.Advance(2);

        }

        /// <summary>
        /// Write signed 32-bit integer (i32) in little-endian format
        /// </summary>
        public void Write(int value)
        {
            var span = _bufferWriter.GetSpan(4);
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            _bufferWriter.Advance(4);

        }

        /// <summary>
        /// Write signed 64-bit integer (i64) in little-endian format
        /// </summary>
        public void Write(long value)
        {
            var span = _bufferWriter.GetSpan(8);
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
            _bufferWriter.Advance(8);

        }

        /// <summary>
        /// Write signed 128-bit integer (i128) in little-endian format
        /// </summary>
        public void Write(Int128 value)
        {
            var span = _bufferWriter.GetSpan(16);
            BinaryPrimitives.WriteInt128LittleEndian(span, value);
            _bufferWriter.Advance(16);

        }

        /// <summary>
        /// Write 128-bit unsigned integer in little-endian format
        /// </summary>
        public void Write(UInt128 value)
        {
            var span = _bufferWriter.GetSpan(16);
            BinaryPrimitives.WriteUInt128LittleEndian(span, value);
            _bufferWriter.Advance(16);

        }

        /// <summary>
        /// Write 256-bit unsigned integer
        /// </summary>
        public void Write(UInt256 value)
        {
            // we must do this instead of direct buffer write because there is internal check for buffer size
            Span<byte> littleEndianBytes = stackalloc byte[32];
            value.ToLittleEndian(littleEndianBytes);

            var span = _bufferWriter.GetSpan(32);
            littleEndianBytes.CopyTo(span);
            _bufferWriter.Advance(32);

        }

        /// <summary>
        /// Write raw bytes from span
        /// </summary>
        public void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            WriteToBuffer(bytes);
        }

        /// <summary>
        /// Write ULEB128 (Variable Length Encoding) integer
        /// </summary>
        public void WriteULEB(UInt128 value)
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

            // Copy to avoid scope issues
            var span = _bufferWriter.GetSpan(bytesWritten);
            tempBuffer.Slice(0, bytesWritten).CopyTo(span);
            _bufferWriter.Advance(bytesWritten);
        }

        /// <summary>
        /// Write ULEB128 from 32-bit uint
        /// </summary>
        public void WriteULEB(uint value)
        {
            WriteULEB(new UInt128(0, value));
        }

        /// <summary>
        /// Write ULEB128 from 64-bit ulong
        /// </summary>
        public void WriteULEB(ulong value)
        {
            WriteULEB(new UInt128(0, value));
        }

        /// <summary>
        /// Write boolean value
        /// </summary>
        public void WriteBool(bool value)
        {
            Write(Convert.ToByte(value));
        }

        /// <summary>
        /// Write string value (length-prefixed UTF-8)
        /// </summary>
        public void WriteString(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteULEB((uint)bytes.Length);
            WriteBytes(bytes);

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
        public void Reset()
        {
            if (!_ownsBuffer)
                throw new InvalidOperationException("Reset is not available when using external IBufferWriter");
            _defaultBuffer!.Clear();

        }

        /// <summary>
        /// Write bytes to the buffer writer
        /// </summary>
        private void WriteToBuffer(ReadOnlySpan<byte> data)
        {
            var span = _bufferWriter.GetSpan(data.Length);
            data.CopyTo(span);
            _bufferWriter.Advance(data.Length);
        }

    }
}