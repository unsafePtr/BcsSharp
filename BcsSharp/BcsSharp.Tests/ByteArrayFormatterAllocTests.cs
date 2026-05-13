using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Verifies the zero-allocation Deserialize path for <see cref="ByteArrayFormatter{T}"/>
/// subclasses that override the <c>ReadOnlySpan&lt;byte&gt;</c> overload. The classic
/// <c>byte[]</c>-only subclass still works (one alloc per deserialize via the default
/// virtual that does <c>bytes.ToArray()</c>).
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
    public void SpanOverride_DeserializeIsZeroAlloc()
    {
        CustomFormatterResolver.Instance.Register<SpanAddress>(new SpanAddressFormatter());
        BcsSerializer.ClearFormatterCache();
        try
        {
            var bytes = BcsSerializer.Serialize(new SpanAddress(0xAB));

            // Use the IBufferWriter overload Reader equivalent: ReadOnlyMemory-based,
            // which is what Deserialize<T>(bytes) already does. Measure deserialize only.
            var perOp = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<SpanAddress>(bytes));

            Console.WriteLine($"Span-overriding ByteArrayFormatter deserialize: {perOp:F1} B/op");
            Assert.Equal(0.0, perOp);
        }
        finally
        {
            CustomFormatterResolver.Instance.Unregister<SpanAddress>();
            BcsSerializer.ClearFormatterCache();
        }
    }

    [Fact]
    public void LegacyByteArrayOverride_DeserializeStillWorks()
    {
        // Backward-compat: a subclass that only implements GetFromBytes(byte[])
        // continues to round-trip correctly (one alloc per deserialize via default virtual).
        CustomFormatterResolver.Instance.Register<LegacyAddress>(new LegacyAddressFormatter());
        BcsSerializer.ClearFormatterCache();
        try
        {
            var original = new LegacyAddress { FirstByte = 0xCD };
            var bytes = BcsSerializer.Serialize(original);
            var back = BcsSerializer.Deserialize<LegacyAddress>(bytes);

            Assert.Equal(original.FirstByte, back.FirstByte);
        }
        finally
        {
            CustomFormatterResolver.Instance.Unregister<LegacyAddress>();
            BcsSerializer.ClearFormatterCache();
        }
    }

    // --- Fixtures -----------------------------------------------------------

    /// <summary>Zero-alloc fixed-size address — single byte payload stored inline.</summary>
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

    /// <summary>Overrides the span overload — copies directly into the struct.</summary>
    public sealed class SpanAddressFormatter : ByteArrayFormatter<SpanAddress>
    {
        public override int GetLength() => SpanAddress.Length;
        public override ReadOnlySpan<byte> GetBytes(SpanAddress value)
        {
            // For the benchmark we only need the first byte to round-trip;
            // pad to Length bytes with zeros via a stack-allocated array.
            Span<byte> buf = stackalloc byte[SpanAddress.Length];
            buf[0] = value.FirstByte;
            return buf.ToArray();  // Serialize is not the focus of this test
        }
        public override SpanAddress GetFromBytes(byte[] bytes) => new(bytes[0]);
        public override SpanAddress GetFromBytes(ReadOnlySpan<byte> bytes) => new(bytes[0]);
    }

    /// <summary>Old-style subclass — only overrides the byte[] overload.</summary>
    public sealed class LegacyAddress : IEquatable<LegacyAddress>
    {
        public byte FirstByte { get; set; }
        public bool Equals(LegacyAddress? other) => other is not null && FirstByte == other.FirstByte;
        public override bool Equals(object? obj) => Equals(obj as LegacyAddress);
        public override int GetHashCode() => FirstByte;
    }

    public sealed class LegacyAddressFormatter : ByteArrayFormatter<LegacyAddress>
    {
        public override int GetLength() => 32;
        public override ReadOnlySpan<byte> GetBytes(LegacyAddress value)
        {
            var buf = new byte[32];
            buf[0] = value.FirstByte;
            return buf;
        }
        public override LegacyAddress GetFromBytes(byte[] bytes) => new() { FirstByte = bytes[0] };
        // Intentionally does NOT override GetFromBytes(ReadOnlySpan<byte>) — uses default.
    }
}
