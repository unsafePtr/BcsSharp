using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core;

/// <summary>
/// Writes BCS-encoded bytes into either an <see cref="IBufferWriter{T}"/> (heap-pooled buffer, auto-grow) or a caller-owned <see cref="Span{T}"/> destination (e.g. a <c>stackalloc</c>'d buffer, fixed size).
/// Over an <see cref="IBufferWriter{T}"/> it keeps the span it was last handed and goes back to the buffer writer only when that span runs out, so a field costs a bounds check rather than two interface calls.
/// Always pass it by <c>ref</c>: a copy tracks its own uncommitted bytes, so writes through the copy and the original overwrite each other.
/// </summary>
public ref struct BcsWriter
{
    private readonly IBufferWriter<byte>? _bufferWriter;
    private Span<byte> _buffer;
    private int _buffered;
    private long _flushed;

    [Obsolete("Construct over an IBufferWriter<byte> or a Span<byte> destination.", error: true)]
    public BcsWriter()
    {
    }

    /// <summary>
    /// Construct over an <see cref="IBufferWriter{T}"/>.
    /// Written bytes reach <paramref name="bufferWriter"/> only on <see cref="Flush"/>; the <see cref="BcsSerializer"/> overloads that take a buffer writer flush for you.
    /// </summary>
    public BcsWriter(IBufferWriter<byte> bufferWriter)
    {
        _bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
    }

    /// <summary>
    /// Construct over a caller-owned <see cref="Span{T}"/>.
    /// The span must outlive this writer (typical pattern: caller does <c>stackalloc byte[N]</c> and passes the span).
    /// Throws on overflow — there is no auto-grow in this mode.
    /// </summary>
    public BcsWriter(Span<byte> destination)
    {
        _buffer = destination;
    }

    /// <summary>
    /// Bytes written so far, including any not yet committed by <see cref="Flush"/>.
    /// </summary>
    public readonly int WrittenCount => checked((int)(_flushed + _buffered));

    /// <summary>
    /// Commits the bytes written since the last flush to the <see cref="IBufferWriter{T}"/>; a no-op over a <see cref="Span{T}"/>.
    /// </summary>
    public void Flush()
    {
        if (_bufferWriter is null || _buffered == 0)
        {
            return;
        }

        _bufferWriter.Advance(_buffered);
        _flushed += _buffered;
        _buffered = 0;

        // IBufferWriter.Advance invalidates every span the buffer writer handed out.
        _buffer = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetWriteSpan(int sizeHint)
    {
        var remaining = _buffer.Length - _buffered;
        if ((uint)remaining < (uint)sizeHint)
        {
            return GetWriteSpanSlow(sizeHint);
        }

        // Slice would repeat the range check the branch above already made.
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), _buffered), remaining);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Span<byte> GetWriteSpanSlow(int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        if (_bufferWriter is null)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"BcsWriter Span<byte> destination is too small (need {sizeHint} more bytes, have {_buffer.Length - _buffered}).");
        }

        Flush();
        _buffer = _bufferWriter.GetSpan(sizeHint);

        if (_buffer.Length < sizeHint)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"The IBufferWriter<byte> returned a {_buffer.Length}-byte span when asked for at least {sizeHint} bytes.");
        }

        return _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdvanceWrite(int count) => _buffered += count;

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

    /// <summary>
    /// Write ULEB128 from 32-bit uint.
    /// Used to encode lengths and enum variant indices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULEB(uint value)
    {
        // Requesting the exact length, not the 5-byte maximum, lets an exactly-sized Span<byte> destination end in a length prefix.
        var length = BitOperations.Log2(value) / 7 + 1;
        var span = GetWriteSpan(length);

        for (var i = 0; i < length - 1; i++)
        {
            span[i] = (byte)(value | 0x80);
            value >>= 7;
        }

        span[length - 1] = (byte)value;
        AdvanceWrite(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        Write(Convert.ToByte(value));
    }

    /// <summary>
    /// Writes a ULEB-length-prefixed UTF-8 string.
    /// BCS has no null string — the optional form is <c>Option&lt;String&gt;</c> — so null is rejected rather than coerced to empty.
    /// Coercing it would encode identically to <c>Option.None</c> for the null case while silently omitting the discriminant for every other value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
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
    public void WritePrimitiveArray<T>(scoped ReadOnlySpan<T> values) where T : unmanaged
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
            var typeSize = Unsafe.SizeOf<T>();
            var span = GetWriteSpan(values.Length * typeSize);
            for (var i = 0; i < values.Length; i++)
            {
                var valueBytes = MemoryMarshal.AsBytes(values.Slice(i, 1));
                for (var j = 0; j < typeSize; j++)
                {
                    span[i * typeSize + j] = valueBytes[typeSize - 1 - j];
                }
            }

            AdvanceWrite(values.Length * typeSize);
        }
    }

    /// <summary>Write raw bytes from a span.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBytes(scoped ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return;
        }

        var span = GetWriteSpan(bytes.Length);
        bytes.CopyTo(span);
        AdvanceWrite(bytes.Length);
    }
}
