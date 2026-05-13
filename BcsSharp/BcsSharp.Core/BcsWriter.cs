using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BcsSharp.Core.Helpers;
using Nethermind.Int256;

namespace BcsSharp.Core;

/// <summary>
/// Writes BCS-encoded bytes into either an <see cref="IBufferWriter{T}"/> (heap-pooled
/// buffer, auto-grow) or a caller-owned <see cref="Span{T}"/> destination (e.g. a
/// <c>stackalloc</c>'d buffer, fixed size).
/// </summary>
public ref struct BcsWriter
{
    private readonly IBufferWriter<byte>? _bufferWriter;
    private Span<byte> _spanDest;
    private int _spanWritten;

    public BcsWriter(IBufferWriter<byte> bufferWriter)
    {
        _bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
        _spanDest = default;
        _spanWritten = 0;
    }

    /// <summary>
    /// Construct over a caller-owned <see cref="Span{T}"/>. The span must outlive this
    /// writer (typical pattern: caller does <c>stackalloc byte[N]</c> and passes the
    /// span). Throws on overflow — there is no auto-grow in this mode.
    /// </summary>
    public BcsWriter(Span<byte> destination)
    {
        _bufferWriter = null;
        _spanDest = destination;
        _spanWritten = 0;
    }

    /// <summary>
    /// Bytes written so far. Only meaningful in <see cref="Span{T}"/> destination mode —
    /// for <see cref="IBufferWriter{T}"/> mode the buffer writer itself tracks this.
    /// </summary>
    public int WrittenCount =>
        _bufferWriter is null
            ? _spanWritten
            : throw new InvalidOperationException("WrittenCount is only available when constructed with a Span<byte> destination.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetWriteSpan(int sizeHint)
    {
        if (_bufferWriter is not null)
        {
            return _bufferWriter.GetSpan(sizeHint);
        }

        if (_spanDest.Length - _spanWritten < sizeHint)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"BcsWriter Span<byte> destination is too small (need {sizeHint} more bytes, have {_spanDest.Length - _spanWritten}).");
        }
        return _spanDest.Slice(_spanWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdvanceWrite(int count)
    {
        if (_bufferWriter is not null)
        {
            _bufferWriter.Advance(count);
        }
        else
        {
            _spanWritten += count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        var span = GetWriteSpan(1);
        span[0] = value;
        AdvanceWrite(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        var span = GetWriteSpan(2);
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        AdvanceWrite(2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        var span = GetWriteSpan(4);
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        AdvanceWrite(4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        var span = GetWriteSpan(8);
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        AdvanceWrite(8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value)
    {
        var span = GetWriteSpan(1);
        span[0] = (byte)value;
        AdvanceWrite(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        var span = GetWriteSpan(2);
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
        AdvanceWrite(2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        var span = GetWriteSpan(4);
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        AdvanceWrite(4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        var span = GetWriteSpan(8);
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        AdvanceWrite(8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Int128 value)
    {
        var span = GetWriteSpan(16);
        BinaryPrimitives.WriteInt128LittleEndian(span, value);
        AdvanceWrite(16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(UInt128 value)
    {
        var span = GetWriteSpan(16);
        BinaryPrimitives.WriteUInt128LittleEndian(span, value);
        AdvanceWrite(16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(UInt256 value)
    {
        // The Nethermind library checks the destination size before writing, so we copy
        // out to a stackalloc'd buffer first.
        Span<byte> littleEndianBytes = stackalloc byte[32];
        value.ToLittleEndian(littleEndianBytes);

        var span = GetWriteSpan(32);
        littleEndianBytes.CopyTo(span);
        AdvanceWrite(32);
    }

    /// <summary>
    /// Write ULEB128 from 32-bit uint. Used to encode lengths and enum variant indices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULEB(uint value)
    {
        Span<byte> span = GetWriteSpan(5); // Max 5 bytes for uint
        int index = 0;

        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                b |= 0x80;
            }
            span[index++] = b;
        } while (value != 0);

        AdvanceWrite(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        Write(Convert.ToByte(value));
    }

    /// <summary>
    /// Write string value (length-prefixed UTF-8) without intermediate allocations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string? value)
    {
        if (value == null || value == string.Empty)
        {
            WriteULEB(0u);
            return;
        }

        var valueSpan = value.AsSpan();
        var byteCount = System.Text.Encoding.UTF8.GetByteCount(valueSpan);
        WriteULEB((uint)byteCount);

        var span = GetWriteSpan(byteCount);
        var actualBytes = System.Text.Encoding.UTF8.GetBytes(valueSpan, span);
        AdvanceWrite(actualBytes);
    }

    /// <summary>
    /// Write primitive array using a direct memory copy on little-endian platforms.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePrimitiveArray<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        if (BitConverter.IsLittleEndian)
        {
            var byteSpan = MemoryMarshal.AsBytes(values);
            var span = GetWriteSpan(byteSpan.Length);
            byteSpan.CopyTo(span);
            AdvanceWrite(byteSpan.Length);
        }
        else
        {
            // Big-endian fallback: reverse byte order per element.
            int typeSize = Unsafe.SizeOf<T>();
            var span = GetWriteSpan(values.Length * typeSize);
            for (int i = 0; i < values.Length; i++)
            {
                var valueBytes = MemoryMarshal.AsBytes(values.Slice(i, 1));
                for (int j = 0; j < typeSize; j++)
                {
                    span[i * typeSize + j] = valueBytes[typeSize - 1 - j];
                }
            }
            AdvanceWrite(values.Length * typeSize);
        }
    }

    /// <summary>Write raw bytes from a span.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0) return;
        var span = GetWriteSpan(bytes.Length);
        bytes.CopyTo(span);
        AdvanceWrite(bytes.Length);
    }
}
