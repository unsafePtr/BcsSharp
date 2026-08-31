using System.Buffers;
using System.Diagnostics;

namespace BcsSharp.Core;

/// <summary>
/// Thin <see cref="IBufferWriter{T}"/> wrapper around an <see cref="ArrayPool{T}.Shared"/> rental.
/// Used by <see cref="BcsSerializer.Serialize{T}(T,IFormatterResolver?)"/> to plug the typed-formatter chain (which expects <see cref="IBufferWriter{T}"/>) into a pool-managed buffer.
///
/// <para>
/// We don't maintain an additional per-thread byte[] cache because <see cref="ArrayPool{T}.Shared"/> already has a per-thread first tier — Rent/Return on the same thread hits that cache and reuses the same array.
/// Re-entrant calls (e.g. nested map serialization) naturally get different arrays from the pool's per-CPU partition.
/// The only thing we pool ourselves is the wrapper instance, via a <c>[ThreadStatic]</c> slot, so even the wrapper object isn't re-allocated.
/// </para>
/// </summary>
internal sealed class ScratchBufferWriter : IBufferWriter<byte>
{
    private const int InitialRentSize = 64 * 1024;

    [ThreadStatic] private static ScratchBufferWriter? t_pooledInstance;

    private byte[] _rented = null!;
    private int _written;

    private ScratchBufferWriter() { }

    /// <summary>
    /// Takes the thread's pooled wrapper, or a fresh one if it is already lent out — which is what a nested serialize (a map inside a map) hits, so the two never share a buffer.
    /// Only <see cref="Return"/> publishes to the slot, and only after releasing its buffer, so anything sitting there is guaranteed free.
    /// </summary>
    public static ScratchBufferWriter Rent()
    {
        var instance = t_pooledInstance ?? new ScratchBufferWriter();

        // Nothing in the slot may still hold a buffer. If it does, some caller returned a
        // live writer and the next two renters are about to share one array.
        Debug.Assert(instance._rented is null, "pooled scratch writer was still holding a buffer");

        t_pooledInstance = null;
        instance._rented = ArrayPool<byte>.Shared.Rent(InitialRentSize);
        instance._written = 0;
        return instance;
    }

    /// <summary>
    /// Releases the buffer and offers the wrapper back to this thread's slot.
    /// Idempotent: a second call must not republish an instance the caller may still be writing to, or the next <see cref="Rent"/> on this thread would hand out a live buffer.
    /// </summary>
    public void Return()
    {
        if (_rented is null)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(_rented);
        _rented = null!;
        _written = 0;
        t_pooledInstance = this;
    }

    public ReadOnlySpan<byte> WrittenSpan => _rented.AsSpan(0, _written);

    internal ReadOnlyMemory<byte> WrittenMemory => _rented.AsMemory(0, _written);

    public int WrittenCount => _written;

    public void Advance(int count)
    {
        if ((uint)count > (uint)(_rented.Length - _written))
        {
            throw new InvalidOperationException("Advance exceeded the buffer's writable region.");
        }
        _written += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _rented.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _rented.AsSpan(_written);
    }

    private void EnsureCapacity(int sizeHint)
    {
        // Writing through a returned writer would otherwise surface as a bare NullReferenceException.
        Debug.Assert(_rented is not null, "scratch writer used after Return");

        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        var needed = sizeHint == 0 ? 1 : sizeHint;
        if (_rented.Length - _written >= needed)
        {
            return;
        }
        Grow(_written + needed);
    }

    private void Grow(int requiredCapacity)
    {
        var newSize = Math.Max(_rented.Length * 2, requiredCapacity);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Buffer.BlockCopy(_rented, 0, newBuffer, 0, _written);
        ArrayPool<byte>.Shared.Return(_rented);
        _rented = newBuffer;
    }
}
