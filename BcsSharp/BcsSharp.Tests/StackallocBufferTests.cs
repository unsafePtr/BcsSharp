using BcsSharp.Core;

namespace BcsSharp.Tests;

/// <summary>
/// Guards the stack-allocated-buffer path through the public reader/writer API: these fail at
/// build time, not run time, if the span parameters stop accepting a <c>stackalloc</c> buffer.
/// </summary>
public class StackallocBufferTests
{
    [Fact]
    public void WriteBytes_AcceptsStackallocBuffer()
    {
        Span<byte> source = stackalloc byte[4] { 0xDE, 0xAD, 0xBE, 0xEF };
        Span<byte> destination = stackalloc byte[4];

        var writer = new BcsWriter(destination);
        writer.WriteBytes(source);

        ReadOnlySpan<byte> expected = [0xDE, 0xAD, 0xBE, 0xEF];
        Assert.Equal(4, writer.WrittenCount);
        Assert.Equal(expected, destination);
    }

    [Fact]
    public void WritePrimitiveArray_AcceptsStackallocBuffer()
    {
        Span<uint> source = stackalloc uint[2] { 1u, 256u };
        Span<byte> destination = stackalloc byte[8];

        var writer = new BcsWriter(destination);
        writer.WritePrimitiveArray<uint>(source);

        ReadOnlySpan<byte> expected = [0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00];
        Assert.Equal(8, writer.WrittenCount);
        Assert.Equal(expected, destination);
    }

    [Fact]
    public void ReadPrimitiveArray_AcceptsStackallocBuffer()
    {
        ReadOnlySpan<byte> encoded = [0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00];
        Span<uint> destination = stackalloc uint[2];

        var reader = new BcsReader(encoded);
        reader.ReadPrimitiveArray(destination);

        ReadOnlySpan<uint> expected = [1u, 256u];
        Assert.Equal(expected, destination);
    }
}
