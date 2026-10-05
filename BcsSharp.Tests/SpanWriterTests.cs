using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// Exercises the <see cref="BcsWriter(Span{byte})"/> constructor — direct serialization into a caller-owned buffer (typically <c>stackalloc</c>'d).
/// No heap involvement at all on the writer side.
/// </summary>
public class SpanWriterTests
{
    [Fact]
    public void Stackalloc_RoundTrip_Primitive()
    {
        Span<byte> buf = stackalloc byte[8];
        var writer = new BcsWriter(buf);
        writer.Write(0x12345678u);

        Assert.Equal(4, writer.WrittenCount);

        var back = BcsSerializer.Deserialize<uint>(buf[..writer.WrittenCount]);
        Assert.Equal(0x12345678u, back);
    }

    [Fact]
    public void Stackalloc_RoundTrip_BcsStruct()
    {
        Span<byte> buf = stackalloc byte[64];
        var writer = new BcsWriter(buf);

        var msg = new Tiny { Id = 42, Flag = true };
        BcsSerializer.Serialize(ref writer, msg);

        var written = writer.WrittenCount;
        var back = BcsSerializer.Deserialize<Tiny>(buf[..written]);

        Assert.Equal(42u, back.Id);
        Assert.True(back.Flag);
    }

    [Fact]
    public void Stackalloc_ByteForByteMatchesByteArrayOverload()
    {
        var msg = new Tiny { Id = 0xDEADBEEFu, Flag = false };

        // Heap-allocating overload (uses ScratchBufferWriter + ArrayPool internally).
        var heapBytes = BcsSerializer.Serialize(msg);

        // Stackalloc destination (no heap).
        Span<byte> stack = stackalloc byte[heapBytes.Length];
        var writer = new BcsWriter(stack);
        BcsSerializer.Serialize(ref writer, msg);

        Assert.Equal(heapBytes.Length, writer.WrittenCount);
        Assert.True(stack[..writer.WrittenCount].SequenceEqual(heapBytes));
    }

    [Fact]
    public void Stackalloc_TooSmall_Throws()
    {
        InvalidOperationException? caught = null;
        try
        {
            // 4 bytes is not enough for a u64.
            Span<byte> buf = stackalloc byte[4];
            var writer = new BcsWriter(buf);
            writer.Write(0xDEADBEEFCAFE0001UL);
        }
        catch (InvalidOperationException ex)
        {
            caught = ex;
        }

        Assert.NotNull(caught);
        Assert.Contains("too small", caught.Message);
    }

    [Fact]
    public void ExactSizeDestination_EndingInLengthPrefix()
    {
        var msg = new Named { Id = 7, Name = "ab" };
        var heapBytes = BcsSerializer.Serialize(msg);

        Span<byte> exact = stackalloc byte[heapBytes.Length];
        var writer = new BcsWriter(exact);
        BcsSerializer.Serialize(ref writer, msg);

        Assert.True(exact.SequenceEqual(heapBytes));
    }

    [BcsStruct]
    public struct Tiny
    {
        [BcsField(0)] public uint Id { get; set; }
        [BcsField(1)] public bool Flag { get; set; }
    }

    [BcsStruct]
    public sealed class Named
    {
        [BcsField(0)] public uint Id { get; set; }
        [BcsField(1)] public string Name { get; set; } = "";
    }
}
