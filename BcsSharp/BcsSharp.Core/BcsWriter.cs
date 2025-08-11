using BcsSharp.Core.Helpers;
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

        public int InitialBufferSize { get; set; } = 1024;
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
            _defaultBuffer = new ArrayBufferWriter<byte>(BcsWriterOptions.Default.InitialBufferSize);
            _bufferWriter = _defaultBuffer;
            _ownsBuffer = true;
        }

        public BcsWriter(BcsWriterOptions options)
        {
            _defaultBuffer = new ArrayBufferWriter<byte>(options.InitialBufferSize);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            var span = _bufferWriter.GetSpan(1);
            span[0] = value;
            _bufferWriter.Advance(1);
        }

        /// <summary>
        /// Write 16-bit unsigned integer (u16) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            var span = _bufferWriter.GetSpan(2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
            _bufferWriter.Advance(2);
        }

        /// <summary>
        /// Write 32-bit unsigned integer (u32) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            var span = _bufferWriter.GetSpan(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
            _bufferWriter.Advance(4);
        }

        /// <summary>
        /// Write 64-bit unsigned integer (u64) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            var span = _bufferWriter.GetSpan(8);
            BinaryPrimitives.WriteUInt64LittleEndian(span, value);
            _bufferWriter.Advance(8);
        }

        /// <summary>
        /// Write signed 8-bit integer (i8)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            var span = _bufferWriter.GetSpan(1);
            span[0] = (byte)value;
            _bufferWriter.Advance(1);
        }

        /// <summary>
        /// Write signed 16-bit integer (i16) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            var span = _bufferWriter.GetSpan(2);
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
            _bufferWriter.Advance(2);
        }

        /// <summary>
        /// Write signed 32-bit integer (i32) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            var span = _bufferWriter.GetSpan(4);
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            _bufferWriter.Advance(4);
        }

        /// <summary>
        /// Write signed 64-bit integer (i64) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            var span = _bufferWriter.GetSpan(8);
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
            _bufferWriter.Advance(8);
        }

        /// <summary>
        /// Write signed 128-bit integer (i128) in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Int128 value)
        {
            var span = _bufferWriter.GetSpan(16);
            BinaryPrimitives.WriteInt128LittleEndian(span, value);
            _bufferWriter.Advance(16);

        }

        /// <summary>
        /// Write 128-bit unsigned integer in little-endian format
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(UInt128 value)
        {
            var span = _bufferWriter.GetSpan(16);
            BinaryPrimitives.WriteUInt128LittleEndian(span, value);
            _bufferWriter.Advance(16);
        }

        /// <summary>
        /// Write 256-bit unsigned integer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(UInt256 value)
        {
            // we must do this instead of direct buffer write because there is internal check for buffer size inside Nethermind library
            Span<byte> littleEndianBytes = stackalloc byte[32];
            value.ToLittleEndian(littleEndianBytes);

            var span = _bufferWriter.GetSpan(32);
            littleEndianBytes.CopyTo(span);
            _bufferWriter.Advance(32);
        }

        /// <summary>
        /// Write ULEB128 from 32-bit uint. Used typically to write down the length of a string or array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteULEB(uint value)
        {
            Span<byte> span = _bufferWriter.GetSpan(5); // Max 5 bytes for uint
            int index = 0;

            do
            {
                byte b = (byte)(value & 0x7F); // Get lowest 7 bits
                value >>= 7; // Shift right by 7 bits

                if (value != 0) // If more bits to encode, set continuation bit
                {
                    b |= 0x80;
                }

                span[index++] = b;
            } while (value != 0);

            _bufferWriter.Advance(index);
        }

        /// <summary>
        /// Write boolean value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value)
        {
            Write(Convert.ToByte(value));
        }

        /// <summary>
        /// Write string value (length-prefixed UTF-8)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string? value)
        {
            if (value == null || value == string.Empty)
            {
                Write((byte)0); // Write length 0 as single byte
                return;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteULEB((uint)bytes.Length);
            WriteBytes(bytes);
        }

        /// <summary>
        /// Get the written data as byte array. Optimized to minimize allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToBytes()
        {
            if (!_ownsBuffer)
            {
                ThrowHelper.ThrowInvalidOperationException("ToBytes is not available when using external IBufferWriter")
            }

            // Smart buffer detection: avoid unnecessary allocations for small data
            var writtenSpan = _defaultBuffer!.WrittenSpan;
            if (writtenSpan.Length == 0)
                return [];

            // For small data, use the most efficient path
            return writtenSpan.ToArray();
        }

        /// <summary>
        /// Gets the written data as ReadOnlyMemory<byte>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ToMemory()
        {
            if (!_ownsBuffer)
            {
                ThrowHelper.ThrowInvalidOperationException("ToMemory is not available when using external IBufferWriter");
            }

            return _defaultBuffer!.WrittenMemory;
        }

        /// <summary>
        /// Reset the writer to empty state
        /// </summary>
        public void Reset()
        {
            if (!_ownsBuffer)
            {
                ThrowHelper.ThrowInvalidOperationException("Reset is not available when using external IBufferWriter");
            }

            _defaultBuffer!.Clear();
        }

        /// <summary>
        /// Write raw bytes from span
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            _bufferWriter.Write(bytes);
        }
    }
}