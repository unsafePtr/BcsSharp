using System.Buffers;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// Exercises the <see cref="BcsWriter(IBufferWriter{byte})"/> constructor, where the writer keeps the buffer writer's span and commits it on <see cref="BcsWriter.Flush"/>.
/// </summary>
public class BufferWriterModeTests
{
    [Fact]
    public void Bytes_ReachBufferWriter_OnFlush()
    {
        var bw = new ArrayBufferWriter<byte>();
        var writer = new BcsWriter(bw);
        writer.Write(1u);
        writer.WriteString("ab");

        Assert.Equal(7, writer.WrittenCount);
        Assert.Equal(0, bw.WrittenCount);

        writer.Flush();

        Assert.Equal(7, writer.WrittenCount);
        Assert.Equal(7, bw.WrittenCount);
    }

    [Fact]
    public void Refills_MatchByteArrayOverload()
    {
        var msg = new Payload
        {
            Id = 0x0102030405060708UL,
            Name = "refill",
            Scores = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1, ["c"] = 3 },
            Values = [1u, 2u, 3u, 4u, 5u],
        };

        var chunked = new ChunkedBufferWriter(chunkSize: 3);
        BcsSerializer.Serialize(chunked, msg);

        Assert.True(chunked.GetSpanCalls > 5);
        Assert.Equal(BcsSerializer.Serialize(msg), chunked.WrittenSpan.ToArray());
    }

    [Fact]
    public void ShortSpanFromBufferWriter_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var writer = new BcsWriter(new ShortSpanBufferWriter());
            writer.Write(1u);
        });

        Assert.Contains("asked for at least 4", ex.Message);
    }

    [BcsStruct]
    public sealed class Payload
    {
        [BcsField(0)] public ulong Id { get; set; }
        [BcsField(1)] public string Name { get; set; } = "";
        [BcsField(2)] public Dictionary<string, int> Scores { get; set; } = [];
        [BcsField(3)] public List<uint> Values { get; set; } = [];
    }

    /// <summary>
    /// Hands out spans of only <c>chunkSize</c> bytes, or the size asked for if larger, so the writer has to refill constantly.
    /// </summary>
    private sealed class ChunkedBufferWriter(int chunkSize) : IBufferWriter<byte>
    {
        private readonly ArrayBufferWriter<byte> _inner = new();

        public int GetSpanCalls { get; private set; }

        public ReadOnlySpan<byte> WrittenSpan => _inner.WrittenSpan;

        public void Advance(int count) => _inner.Advance(count);

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var size = Math.Max(sizeHint, chunkSize);

            return _inner.GetMemory(size)[..size];
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            GetSpanCalls++;
            var size = Math.Max(sizeHint, chunkSize);

            return _inner.GetSpan(size)[..size];
        }
    }

    /// <summary>
    /// Breaks the <see cref="IBufferWriter{T}"/> contract by returning fewer bytes than asked for.
    /// </summary>
    private sealed class ShortSpanBufferWriter : IBufferWriter<byte>
    {
        private readonly byte[] _buffer = new byte[2];

        public void Advance(int count)
        {
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => _buffer;

        public Span<byte> GetSpan(int sizeHint = 0) => _buffer;
    }
}
