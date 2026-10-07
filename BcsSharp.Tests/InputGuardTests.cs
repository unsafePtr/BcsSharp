using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// Decode guards that keep crafted input from crashing or exhausting the process, rejecting what Rust's <c>bcs</c> rejects.
/// </summary>
public class InputGuardTests
{
    // ULEB128 of 2^31, one past MaxSequenceLength.
    private static readonly byte[] TooLong = [0x80, 0x80, 0x80, 0x80, 0x08];

    // ULEB128 of 2^31 - 1: five bytes claiming the largest legal length.
    private static readonly byte[] LargestLength = [0xFF, 0xFF, 0xFF, 0xFF, 0x07];

    // The recursive fixture uses a hand-written formatter: it exercises the EnterContainer/LeaveContainer pair a custom formatter for a recursive type must call, which a resolved [BcsStruct] (see RecursiveTypeTests) does on its own.
    private static readonly IFormatterResolver NodeResolver = CreateNodeResolver();

    [Fact]
    public void ChainAtTheDepthLimit_RoundTrips()
    {
        var bytes = BcsSerializer.Serialize(Chain(BcsSerializer.MaxContainerDepth), NodeResolver);

        Assert.Equal(ChainBytes(BcsSerializer.MaxContainerDepth), bytes);
        Assert.Equal(BcsSerializer.MaxContainerDepth, Depth(BcsSerializer.Deserialize<Node>(bytes, NodeResolver)));
    }

    [Fact]
    public void ChainPastTheDepthLimit_IsRejected()
    {
        var bytes = ChainBytes(BcsSerializer.MaxContainerDepth + 1);

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<Node>(bytes, NodeResolver));

        Assert.Contains("container depth", ex.Message);
    }

    [Fact]
    public void DeeplyNestedInput_ThrowsInsteadOfOverflowingTheStack()
    {
        // A million levels would overflow any stack; without the limit this test takes the process down rather than failing.
        var bytes = ChainBytes(1_000_000);

        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<Node>(bytes, NodeResolver));
    }

    [Fact]
    public void LoweredDepthLimit_IsEnforced()
    {
        var bytes = ChainBytes(11);

        Assert.Equal(11, Depth(ReadWithLimit<Node>(bytes, 11)));
        Assert.Throws<InvalidOperationException>(() => ReadWithLimit<Node>(bytes, 10));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(BcsSerializer.MaxContainerDepth + 1)]
    public void DepthLimit_CanOnlyBeLowered(int maxContainerDepth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new BcsReader(Array.Empty<byte>(), maxContainerDepth); });
    }

    [Fact]
    public void EnumsCountTowardDepth()
    {
        // The struct is one level and its enum field a second: Rust enters a container for unit variants too.
        byte[] bytes = [0x01];

        Assert.Equal(Color.Green, ReadWithLimit<WithColor>(bytes, 2).Color);
        Assert.Throws<InvalidOperationException>(() => ReadWithLimit<WithColor>(bytes, 1));
    }

    [Fact]
    public void UnionsCountTowardDepth()
    {
        byte[] bytes = [0x00];

        Assert.IsType<Empty>(ReadWithLimit<Shape>(bytes, 1).Value);
        Assert.Throws<InvalidOperationException>(() => ReadWithLimit<Shape>(bytes, 0));
    }

    [Fact]
    public void OptionsAndListsDoNotCountTowardDepth()
    {
        // One struct holding Some("hi") and [7]: Rust counts neither the Option nor the Vec, so a limit of 1 is enough.
        byte[] bytes = [0x01, 0x02, 0x68, 0x69, 0x01, 0x07, 0x00, 0x00, 0x00];

        var value = ReadWithLimit<WithOptionAndList>(bytes, 1);

        Assert.Equal("hi", value.Name is string name ? name : null);
        Assert.Equal(new[] { 7u }, value.Numbers);
    }

    [Fact]
    public void LengthAboveMaxSequenceLength_IsRejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<string>(TooLong));

        Assert.Contains("exceeds the BCS maximum", ex.Message);
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<List<Circle>>(TooLong));
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<List<long>>(TooLong));
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<Dictionary<long, long>>(TooLong));
    }

    [Fact]
    public void ForgedLength_DoesNotAllocateTheClaimedSize()
    {
        AssertRejectedCheaply(() => BcsSerializer.Deserialize<List<long>>(LargestLength));
        AssertRejectedCheaply(() => BcsSerializer.Deserialize<List<Circle>>(LargestLength));
        AssertRejectedCheaply(() => BcsSerializer.Deserialize<Dictionary<long, long>>(LargestLength));
    }

    [Fact]
    public void ZeroSizeElements_AreNotLimitedByTheInputSize()
    {
        // A unit encodes to nothing, so a thousand of them are just the length prefix; Rust accepts this, so the capacity cap must not reject it.
        byte[] bytes = [0xE8, 0x07];

        Assert.Equal(1000, BcsSerializer.Deserialize<List<Unit>>(bytes).Count);
    }

    [Fact]
    public void LeftoverBytes_AreRejected()
    {
        byte[] bytes = [0x2A, 0x00, 0x00, 0x00, 0xFF];

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<uint>(bytes));

        Assert.Contains("1 bytes remain", ex.Message);
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<uint>(bytes.AsMemory()));
        Assert.Throws<InvalidOperationException>(() =>
        {
            var value = 0u;
            BcsSerializer.Deserialize(bytes.AsSpan(), ref value);
        });
    }

    [Fact]
    public void ExactInput_IsAccepted()
    {
        byte[] bytes = [0x2A, 0x00, 0x00, 0x00];

        Assert.Equal(42u, BcsSerializer.Deserialize<uint>(bytes));
    }

    [Fact]
    public void ReaderOverload_StillReadsValuesBackToBack()
    {
        byte[] bytes = [0x2A, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00];
        var reader = new BcsReader(bytes);

        Assert.Equal(42u, BcsSerializer.Deserialize<uint>(ref reader));
        Assert.Equal(7u, BcsSerializer.Deserialize<uint>(ref reader));
        Assert.False(reader.HasRemainingBytes);
    }

    [Fact]
    public void BoolList_RejectsBytesOtherThanZeroOrOne()
    {
        byte[] bytes = [0x03, 0x01, 0x02, 0x00];

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<List<bool>>(bytes));

        Assert.Contains("Invalid boolean value: 2", ex.Message);
        Assert.Throws<InvalidOperationException>(() =>
        {
            var list = new List<bool>();
            BcsSerializer.Deserialize(bytes.AsSpan(), ref list);
        });
    }

    [Fact]
    public void BoolList_AcceptsZeroAndOne()
    {
        byte[] bytes = [0x02, 0x00, 0x01];

        Assert.Equal(new[] { false, true }, BcsSerializer.Deserialize<List<bool>>(bytes));
    }

    private static Node Chain(int depth)
    {
        var node = new Node();
        for (var i = 1; i < depth; i++)
        {
            node = new Node { Next = node };
        }

        return node;
    }

    // Every level but the innermost is Some (0x01) followed by the next node; the innermost Next is None (0x00).
    private static byte[] ChainBytes(int depth)
    {
        var bytes = new byte[depth];
        bytes.AsSpan(0, depth - 1).Fill(0x01);

        return bytes;
    }

    private static int Depth(Node node)
    {
        var depth = 1;
        while (node.Next is Node next)
        {
            node = next;
            depth++;
        }

        return depth;
    }

    [Fact]
    public void StringLengthOfIntMaxValue_ThrowsEndOfStream()
    {
        // The prefix itself moves the position past zero, so position + length overflows int.
        Assert.Throws<EndOfStreamException>(() =>
        {
            ReadOnlySpan<byte> bytes = [0xFF, 0xFF, 0xFF, 0xFF, 0x07];
            BcsSerializer.Deserialize<string>(bytes);
        });
    }

    private static T ReadWithLimit<T>(byte[] bytes, int maxContainerDepth)
    {
        var reader = new BcsReader(bytes, maxContainerDepth);

        return BcsSerializer.Deserialize<T>(ref reader, NodeResolver);
    }

    private static IFormatterResolver CreateNodeResolver()
    {
        var custom = new CustomFormatterResolver();
        custom.Register(new NodeFormatter());

        return CompositeResolver.Create(custom);
    }

    private static void AssertRejectedCheaply(Action deserialize)
    {
        // The first call builds and caches the formatters, which allocates; only the second is measured.
        Assert.Throws<EndOfStreamException>(deserialize);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<EndOfStreamException>(deserialize);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Sizing the collection from the prefix would ask for gigabytes; what is left is the exception itself.
        Assert.True(allocated < 64 * 1024, $"Allocated {allocated:N0} bytes for a rejected input");
    }

    // Wire-identical to Rust's `struct Node { next: Option<Box<Node>> }`.
    public sealed class Node
    {
        public Node? Next { get; set; }
    }

    private sealed class NodeFormatter : IBcsFormatter<Node>
    {
        public void Serialize(ref BcsWriter writer, Node value)
        {
            if (value.Next is null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            Serialize(ref writer, value.Next);
        }

        public Node Deserialize(ref BcsReader reader)
        {
            reader.EnterContainer();

            var node = new Node();
            var tag = reader.Read8();

            if (tag == 1)
            {
                node.Next = Deserialize(ref reader);
            }
            else if (tag != 0)
            {
                throw new InvalidOperationException($"Invalid Option discriminant: {tag}.");
            }

            reader.LeaveContainer();

            return node;
        }
    }

    public enum Color { Red, Green }

    [BcsStruct]
    public sealed class WithColor
    {
        [BcsField(0)] public Color Color { get; set; }
    }

    [BcsStruct]
    public sealed class WithOptionAndList
    {
        [BcsField(0)] public Option<string> Name { get; set; } = None.Instance;
        [BcsField(1)] public List<uint> Numbers { get; set; } = new();
    }

    public sealed record class Empty;

    [BcsStruct]
    public sealed record class Circle
    {
        [BcsField(0)] public uint Radius { get; set; }
    }

    public union Shape(Empty, Circle);
}
