using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Exercises the thread-static scratch path used by <see cref="BcsSerializer.Serialize{T}(T,IFormatterResolver?)"/>.
/// Covers small payloads (stay in scratch), large payloads (overflow to ArrayPool), and re-entrant serialization (nested call must not clobber the outer scratch contents).
/// </summary>
public class ScratchBufferTests
{
    [Fact]
    public void SmallPayload_RoundTrips()
    {
        var values = new List<int> { 1, 2, 3, 4, 5 };

        var bytes = BcsSerializer.Serialize(values);
        var back = BcsSerializer.Deserialize<List<int>>(bytes);

        Assert.Equal(values, back);
    }

    [Fact]
    public void LargePayload_OverflowsScratch_RoundTrips()
    {
        // 64 KB scratch + ULEB length prefix means a 200 000-element int list (~800 KB)
        // forces several grow steps onto the ArrayPool path.
        var values = new List<int>(200_000);
        for (int i = 0; i < 200_000; i++)
        {
            values.Add(i);
        }

        var bytes = BcsSerializer.Serialize(values);
        var back = BcsSerializer.Deserialize<List<int>>(bytes);

        Assert.Equal(values.Count, back.Count);
        Assert.Equal(values[0], back[0]);
        Assert.Equal(values[^1], back[^1]);
    }

    [Fact]
    public void RepeatedSerializations_ReuseScratch()
    {
        // After the first call, the thread-static scratch is initialised. Each subsequent
        // call must produce a fresh, correct payload — i.e. Return() resets state cleanly.
        for (int i = 0; i < 20; i++)
        {
            var bytes = BcsSerializer.Serialize<uint>((uint)i);
            var back = BcsSerializer.Deserialize<uint>(bytes);
            Assert.Equal((uint)i, back);
            Assert.Equal(4, bytes.Length);
        }
    }

    [Fact]
    public void Reentrant_NestedSerializeFromFormatter_DoesNotCorruptOuter()
    {
        // The formatter for the outer type performs its own nested BcsSerializer.Serialize
        // call while the outer scratch is still active. The nested call must transparently
        // fall back to a pooled buffer.
        var custom = new CustomFormatterResolver();
        custom.Register<Outer>(new OuterFormatter());
        var resolver = CompositeResolver.Create(custom);

        var outer = new Outer(123, "nested");
        var bytes = BcsSerializer.Serialize(outer, resolver);
        var back = BcsSerializer.Deserialize<Outer>(bytes, resolver);

        Assert.Equal(outer.Id, back.Id);
        Assert.Equal(outer.Name, back.Name);
    }

    [Fact]
    public void ConcurrentThreads_ProduceIndependentResults()
    {
        // The scratch buffer is [ThreadStatic], so each thread must hold its own. Run
        // many serializations across many threads simultaneously and verify each thread's
        // output is correct for its own input — no cross-thread contamination.
        const int threadCount = 16;
        const int iterationsPerThread = 1_000;

        var failures = 0;
        var threads = new Thread[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            var threadId = t;
            threads[t] = new Thread(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    // Distinct payload per (thread, iteration). If two threads were
                    // sharing scratch, the round-trip would mismatch.
                    var value = (ulong)threadId * 1_000_000UL + (uint)i;
                    var bytes = BcsSerializer.Serialize(value);
                    var back = BcsSerializer.Deserialize<ulong>(bytes);
                    if (back != value)
                    {
                        Interlocked.Increment(ref failures);
                    }
                }
            });
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        Assert.Equal(0, failures);
    }

    [Fact]
    public void ExceptionDuringSerialize_ReleasesScratch_NextCallSucceeds()
    {
        // Register a formatter that always throws. If Serialize<T>'s finally{Return()}
        // is wired correctly, the buffer goes back to the pool and the next call on this
        // thread can claim the scratch again — and produce a correct payload.
        var custom = new CustomFormatterResolver();
        custom.Register<ThrowingMarker>(new ThrowingFormatter());
        var resolver = CompositeResolver.Create(custom);

        Assert.Throws<InvalidOperationException>(() =>
            BcsSerializer.Serialize(new ThrowingMarker(), resolver));

        // Same thread, immediately after — must not see "scratch already in use".
        var bytes = BcsSerializer.Serialize<uint>(0xDEADBEEF, resolver);
        var back = BcsSerializer.Deserialize<uint>(bytes, resolver);
        Assert.Equal(0xDEADBEEFu, back);
    }

    public sealed record ThrowingMarker;

    private sealed class ThrowingFormatter : IBcsFormatter<ThrowingMarker>
    {
        public void Serialize(ref BcsWriter writer, ThrowingMarker value)
        {
            // Write something first so the buffer is non-empty when we bail.
            writer.Write((uint)0xC0FFEE);
            throw new InvalidOperationException("simulated formatter failure");
        }

        public ThrowingMarker Deserialize(ref BcsReader reader)
            => throw new NotSupportedException();
    }

    public sealed record Outer(uint Id, string Name);

    private sealed class OuterFormatter : IBcsFormatter<Outer>
    {
        public void Serialize(ref BcsWriter writer, Outer value)
        {
            writer.Write(value.Id);

            // Nested top-level call — must rent a separate buffer.
            var nameBytes = BcsSerializer.Serialize(value.Name);
            writer.WriteBytes(nameBytes);
        }

        public Outer Deserialize(ref BcsReader reader)
        {
            var id = reader.Read32();
            var name = reader.ReadString();
            return new Outer(id, name);
        }
    }
}
