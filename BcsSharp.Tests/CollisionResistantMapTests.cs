using BcsSharp.Core;
using BcsSharp.Core.Helpers;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

public class CollisionResistantMapTests
{
    private static readonly IFormatterResolver Resistant = CompositeResolver.Create(CollisionResistantMapResolver.Instance);

    // Reference vectors from the SipHash paper: key 00..0f, message 00..(n-1).
    [Theory]
    [InlineData(0, 0x726fdb47dd0e0e31UL)]
    [InlineData(15, 0xa129ca6149be45e5UL)]
    public void SipHash_MatchesReferenceVectors(int length, ulong expected)
    {
        var hash = new SipHash(0x0706050403020100UL, 0x0f0e0d0c0b0a0908UL);
        var message = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();

        Assert.Equal(expected, hash.Compute(message));
    }

    [Fact]
    public void For_KeepsDefaultForStringsAndCustomTypes()
    {
        Assert.Null(CollisionResistantComparer.For<string>());
        Assert.Null(CollisionResistantComparer.For<Guid>());
        Assert.NotNull(CollisionResistantComparer.For<long>());
        Assert.NotNull(CollisionResistantComparer.For<DayOfWeek>());
    }

    [Fact]
    public void KeysThatCollideByDefault_HashApart()
    {
        // long.GetHashCode XORs its two halves, so every (x << 32) | x hashes to 0.
        var comparer = CollisionResistantComparer.For<long>()!;
        Assert.Equal(0L.GetHashCode(), ((1L << 32) | 1L).GetHashCode());

        Assert.NotEqual(comparer.GetHashCode(0L), comparer.GetHashCode((1L << 32) | 1L));
    }

    [Fact]
    public void OptedInChain_DecodesWithResistantComparer()
    {
        var map = new Dictionary<long, string> { [1] = "a", [2] = "b" };
        var bytes = BcsSerializer.Serialize(map);

        var decoded = BcsSerializer.Deserialize<Dictionary<long, string>>(bytes, Resistant);

        Assert.Same(CollisionResistantComparer.For<long>(), decoded.Comparer);
        Assert.Equal(map, decoded);
    }

    [Fact]
    public void DefaultChain_KeepsDefaultComparer()
    {
        var bytes = BcsSerializer.Serialize(new Dictionary<long, string> { [1] = "a" });

        var decoded = BcsSerializer.Deserialize<Dictionary<long, string>>(bytes);

        Assert.Same(EqualityComparer<long>.Default, decoded.Comparer);
    }
}
