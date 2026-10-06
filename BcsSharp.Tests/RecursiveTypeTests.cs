using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// A type that refers to itself asks the chain for its own formatter while that formatter is still being built.
/// The chain answers the re-entrant request with a stand-in that binds once construction is done, so recursive types such as Sui's <c>TypeTag</c> resolve instead of overflowing the stack.
/// </summary>
public class RecursiveTypeTests
{
    [Fact]
    public void SelfReferenceThroughOption_RoundTrips()
    {
        var chain = new Node { Next = new Node { Next = new Node() } };

        var bytes = BcsSerializer.Serialize(chain);

        // Wire-identical to Rust's `struct Node { next: Option<Box<Node>> }`: Some, Some, None.
        Assert.Equal(new byte[] { 0x01, 0x01, 0x00 }, bytes);
        Assert.Equal(3, Depth(BcsSerializer.Deserialize<Node>(bytes)));
    }

    [Fact]
    public void SelfReferenceThroughList_RoundTrips()
    {
        var tree = new Tree
        {
            Value = 1,
            Children = [new Tree { Value = 2 }, new Tree { Value = 3, Children = [new Tree { Value = 4 }] }],
        };

        var back = BcsSerializer.Deserialize<Tree>(BcsSerializer.Serialize(tree));

        Assert.Equal(1u, back.Value);
        Assert.Equal(new[] { 2u, 3u }, back.Children.Select(c => c.Value).ToArray());
        Assert.Equal(4u, back.Children[1].Children[0].Value);
    }

    [Fact]
    public void RecursiveUnion_RoundTrips()
    {
        // vector<vector<bool>> as Sui's TypeTag spells it: Vector(Vector(Bool)).
        TypeTag tag = new Vector { Element = new Vector { Element = new Bool() } };

        var bytes = BcsSerializer.Serialize(tag);

        Assert.Equal(new byte[] { 0x01, 0x01, 0x00 }, bytes);

        var back = BcsSerializer.Deserialize<TypeTag>(bytes);
        var inner = Assert.IsType<Vector>(back.Value);
        var innermost = Assert.IsType<Vector>(inner.Element.Value);
        Assert.IsType<Bool>(innermost.Element.Value);
    }

    [Fact]
    public void MutualRecursion_RoundTrips()
    {
        var ping = new Ping { Pong = new Pong { Ping = new Ping() } };

        var bytes = BcsSerializer.Serialize(ping);

        Assert.Equal(new byte[] { 0x01, 0x01, 0x00 }, bytes);
        Assert.IsType<Pong>(BcsSerializer.Deserialize<Ping>(bytes).Pong.Value);
    }

    [Fact]
    public void DepthLimit_StillApplies()
    {
        Assert.Equal(BcsSerializer.MaxContainerDepth, Depth(BcsSerializer.Deserialize<Node>(ChainBytes(BcsSerializer.MaxContainerDepth))));

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<Node>(ChainBytes(BcsSerializer.MaxContainerDepth + 1)));

        Assert.Equal("Structs and enums are nested deeper than the container depth limit (at most 500).", ex.Message);
    }

    [Fact]
    public void OnlyTheBackEdgeUsesTheStandIn()
    {
        // The chain hands out the real formatter; the stand-in is only what that formatter holds for its own field.
        Assert.IsType<BcsObjectFormatter<Node>>(BcsSerializer.GetFormatter<Node>());
    }

    [Fact]
    public void ScopedChain_ResolvesARecursiveTypeOnItsOwn()
    {
        var chain = CompositeResolver.Create();

        var bytes = BcsSerializer.Serialize(new Node { Next = new Node() }, chain);

        Assert.Equal(new byte[] { 0x01, 0x00 }, bytes);
        Assert.IsType<BcsObjectFormatter<Node>>(chain.GetFormatter<Node>());
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

    // Every level but the innermost is Some (0x01) followed by the next node; the innermost Next is None (0x00).
    private static byte[] ChainBytes(int depth)
    {
        var bytes = new byte[depth];
        bytes.AsSpan(0, depth - 1).Fill(0x01);

        return bytes;
    }

    [BcsStruct]
    public sealed class Node
    {
        [BcsField(0)] public Option<Node> Next { get; set; } = None.Instance;
    }

    [BcsStruct]
    public sealed class Tree
    {
        [BcsField(0)] public uint Value { get; set; }
        [BcsField(1)] public List<Tree> Children { get; set; } = [];
    }

    public sealed record class Bool;

    [BcsStruct]
    public sealed class Vector
    {
        [BcsField(0)] public TypeTag Element { get; set; }
    }

    public union TypeTag(Bool, Vector);

    [BcsStruct]
    public sealed class Ping
    {
        [BcsField(0)] public Option<Pong> Pong { get; set; } = None.Instance;
    }

    [BcsStruct]
    public sealed class Pong
    {
        [BcsField(0)] public Option<Ping> Ping { get; set; } = None.Instance;
    }
}
