using System.Buffers;

namespace BcsSharp.Core;

/// <summary>
/// Thread-static scratch buffer + <see cref="ArrayPool{T}"/> overflow, modeled on
/// MessagePack-CSharp's two-tier writer strategy:
///
/// 1. A lazy-initialised 64 KB <c>[ThreadStatic]</c> byte array serves as scratch for the
///    common-case payload, reused across calls on the same thread with no allocation.
/// 2. If a serialization overruns 64 KB (or another serialization on the same thread is
///    already holding the scratch), the writer migrates / falls back to a buffer rented
///    from <see cref="ArrayPool{T}.Shared"/>. The rented buffer is returned on
///    <see cref="Return"/>.
///
/// The wrapper instance is itself pooled in a thread-static slot so the hot path is
/// allocation-free end-to-end.
/// </summary>
internal sealed class ScratchBufferWriter : IBufferWriter<byte>
{
    private const int ScratchSize = 65536;

    [ThreadStatic]
    private static byte[]? t_scratch;
    [ThreadStatic]
    private static bool t_scratchInUse;
    [ThreadStatic]
    private static ScratchBufferWriter? t_pooledInstance;

    private byte[] _buffer = null!;
    private bool _usingScratch;
    private byte[]? _rented;
    private int _written;

    private ScratchBufferWriter() { }

    /// <summary>
    /// Acquire a writer for the current thread. The first call on a thread allocates the
    /// 64 KB scratch; subsequent calls reuse it. A re-entrant call (e.g. one
    /// <c>Serialize</c> invoked from inside another's formatter) falls back to a pooled
    /// buffer so the in-flight scratch is not clobbered.
    /// </summary>
    public static ScratchBufferWriter Rent()
    {
        var instance = t_pooledInstance ?? new ScratchBufferWriter();
        t_pooledInstance = null;
        instance.Initialize();
        return instance;
    }

    /// <summary>
    /// Return the writer and any rented overflow buffer. Safe to call multiple times.
    /// </summary>
    public void Return()
    {
        if (_rented is not null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
            _rented = null;
        }

        if (_usingScratch)
        {
            t_scratchInUse = false;
            _usingScratch = false;
        }

        _buffer = null!;
        _written = 0;

        // Pool the wrapper itself so the next Rent() on this thread is allocation-free.
        t_pooledInstance = this;
    }

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

    /// <summary>
    /// Heap-friendly view over the written region. Exposed so callers that need to hold
    /// the buffer reference across a non-ref-struct boundary (e.g. an <c>IComparer&lt;T&gt;</c>
    /// for <c>Span&lt;T&gt;.Sort</c>) can do so without copying.
    /// </summary>
    internal ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

    public int WrittenCount => _written;

    public void Advance(int count)
    {
        if ((uint)count > (uint)(_buffer.Length - _written))
        {
            throw new InvalidOperationException("Advance exceeded the buffer's writable region.");
        }

        _written += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void Initialize()
    {
        if (!t_scratchInUse)
        {
            t_scratch ??= new byte[ScratchSize];
            _buffer = t_scratch;
            _usingScratch = true;
            t_scratchInUse = true;
        }
        else
        {
            // Re-entrant on the same thread; scratch is already in flight. Rent a fresh
            // buffer so the outer writer's contents stay intact.
            _rented = ArrayPool<byte>.Shared.Rent(ScratchSize);
            _buffer = _rented;
            _usingScratch = false;
        }

        _written = 0;
    }

    private void EnsureCapacity(int sizeHint)
    {
        // IBufferWriter contract: a hint of 0 means "at least one byte". A negative hint
        // is an argument error.
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        var needed = sizeHint == 0 ? 1 : sizeHint;
        var available = _buffer.Length - _written;
        if (available >= needed)
        {
            return;
        }

        Grow(_written + needed);
    }

    private void Grow(int requiredCapacity)
    {
        var newSize = Math.Max(_buffer.Length * 2, requiredCapacity);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _written);

        if (_rented is not null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
        }
        else if (_usingScratch)
        {
            // Migrated off the scratch slot, but we still hold the in-use flag for the
            // duration of this serialization — releasing it now would let a nested call
            // reuse the same migrated buffer's predecessor space mid-flight.
            _usingScratch = false;
        }

        _buffer = newBuffer;
        _rented = newBuffer;
    }
}
