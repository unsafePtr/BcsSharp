using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using System.Buffers;

namespace BcsSharp.Tests;

/// <summary>
/// Verifies the <see cref="BcsSerializer.Deserialize{T}(ReadOnlySpan{byte}, IFormatterResolver?)"/> overload — zero-copy entry point for callers holding stack-allocated buffers, slices of larger arrays, or any other span source.
/// </summary>
public class SpanDeserializeTests
{
    [Fact]
    public void Deserialize_FromReadOnlySpan_RoundTrips()
    {
        var original = new Point { X = 7, Y = 13 };
        byte[] bytes = BcsSerializer.Serialize(original);

        // Pass as ReadOnlySpan<byte>.
        ReadOnlySpan<byte> span = bytes;
        var back = BcsSerializer.Deserialize<Point>(span);

        Assert.Equal(7u, back.X);
        Assert.Equal(13u, back.Y);
    }

    [Fact]
    public void Deserialize_FromStackallocSpan_RoundTrips()
    {
        Span<byte> dest = stackalloc byte[8];
        var bw = new ArrayBufferWriter<byte>(8);
        BcsSerializer.Serialize(bw, new Point { X = 42, Y = 99 });
        bw.WrittenSpan.CopyTo(dest);

        // The caller can deserialize directly from the stack-allocated span — no copy
        // into a byte[] or wrap in ReadOnlyMemory needed.
        var back = BcsSerializer.Deserialize<Point>((ReadOnlySpan<byte>)dest);

        Assert.Equal(42u, back.X);
        Assert.Equal(99u, back.Y);
    }

    [Fact]
    public void Deserialize_FromSlice_OfLargerBuffer()
    {
        var original = new Point { X = 1, Y = 2 };
        byte[] payload = BcsSerializer.Serialize(original);

        // Embed the payload inside a larger buffer with arbitrary leading bytes.
        var combined = new byte[16];
        Array.Fill(combined, (byte)0xFF);
        payload.CopyTo(combined.AsSpan(4));

        var slice = combined.AsSpan(4, payload.Length);
        var back = BcsSerializer.Deserialize<Point>(slice);

        Assert.Equal(1u, back.X);
        Assert.Equal(2u, back.Y);
    }

    [Fact]
    public void Deserialize_ReadOnlyMemory_StillWorks_NoRegression()
    {
        // Sanity: the existing ReadOnlyMemory overload still resolves and round-trips
        // after the ReadOnlySpan overload was added.
        var original = new Point { X = 5, Y = 6 };
        ReadOnlyMemory<byte> mem = BcsSerializer.Serialize(original);
        var back = BcsSerializer.Deserialize<Point>(mem);

        Assert.Equal(5u, back.X);
        Assert.Equal(6u, back.Y);
    }

    [BcsStruct]
    public struct Point
    {
        [BcsField(0)] public uint X { get; set; }
        [BcsField(1)] public uint Y { get; set; }
    }
}
