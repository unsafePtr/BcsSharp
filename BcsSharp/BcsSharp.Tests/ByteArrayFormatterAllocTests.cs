using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Verifies that <see cref="ByteArrayFormatter{T}"/> subclasses deserialize allocation-free.
/// The base class' only abstract Deserialize hook is <c>GetFromBytes(ReadOnlySpan&lt;byte&gt;)</c>,
/// so every subclass automatically gets the zero-alloc path — no opt-in required.
/// </summary>
public class ByteArrayFormatterAllocTests
{
    const int Iters = 100_000;

    private static double MeasureBytesPerOp(Action work)
    {
        for (int i = 0; i < 1_000; i++) work();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iters; i++) work();
        var after = GC.GetAllocatedBytesForCurrentThread();
        return (after - before) / (double)Iters;
    }

    [Fact]
    public void Deserialize_IsZeroAlloc()
    {
        CustomFormatterResolver.Instance.Register<SpanAddress>(new SpanAddressFormatter());
        BcsSerializer.ClearFormatterCache();
        try
        {
            var bytes = BcsSerializer.Serialize(new SpanAddress(0xAB));

            var perOp = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<SpanAddress>(bytes));

            Console.WriteLine($"ByteArrayFormatter deserialize: {perOp:F4} B/op");
            // Effectively zero. The tiny upper bound tolerates one-off JIT codegen
            // landing inside the measurement window on Debug builds.
            Assert.True(perOp < 0.01, $"Expected ~0 B/op, got {perOp}");
        }
        finally
        {
            CustomFormatterResolver.Instance.Unregister<SpanAddress>();
            BcsSerializer.ClearFormatterCache();
        }
    }

    // --- Fixtures -----------------------------------------------------------

    public readonly struct SpanAddress : IEquatable<SpanAddress>
    {
        public const int Length = 32;
        private readonly byte _firstByte;
        public SpanAddress(byte firstByte) => _firstByte = firstByte;
        public byte FirstByte => _firstByte;
        public bool Equals(SpanAddress other) => _firstByte == other._firstByte;
        public override bool Equals(object? obj) => obj is SpanAddress a && Equals(a);
        public override int GetHashCode() => _firstByte;
    }

    public sealed class SpanAddressFormatter : ByteArrayFormatter<SpanAddress>
    {
        public override int GetLength() => SpanAddress.Length;
        public override ReadOnlySpan<byte> GetBytes(SpanAddress value)
        {
            // Serialize cost is not the focus of this test; the heap allocation here
            // is unrelated to the Deserialize hot path being measured.
            var buf = new byte[SpanAddress.Length];
            buf[0] = value.FirstByte;
            return buf;
        }
        public override SpanAddress GetFromBytes(ReadOnlySpan<byte> bytes) => new(bytes[0]);
    }
}
