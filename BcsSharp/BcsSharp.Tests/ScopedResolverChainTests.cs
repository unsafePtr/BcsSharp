using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Composed formatters capture their children, so they belong to the chain that built them.
/// Two chains overriding the same type must not see each other's formatters, and neither may leak into the default chain.
/// </summary>
public class ScopedResolverChainTests
{
    private static CompositeResolver ChainOverriding(byte marker)
    {
        var custom = new CustomFormatterResolver();
        custom.Register<Probe>(new StubFormatter(marker));
        return CompositeResolver.Create(custom);
    }

    private static string Hex(Dictionary<string, Probe> map, IFormatterResolver? resolver) =>
        Convert.ToHexString(BcsSerializer.Serialize(map, resolver));

    [Fact]
    public void ChainsWithDifferentOverridesForTheSameTypeStayIsolated()
    {
        var map = new Dictionary<string, Probe> { ["k"] = new() { Value = 1 } };

        var chainA = ChainOverriding(0xAA);
        var chainB = ChainOverriding(0xBB);

        // Value byte differs per chain; the default chain keeps the [BcsStruct] encoding.
        Assert.Equal("01016BAA", Hex(map, chainA));
        Assert.Equal("01016BBB", Hex(map, chainB));
        Assert.Equal("01016B01000000", Hex(map, null));

        // Re-check in the reverse order: whichever resolved first must not have poisoned
        // the others, which is exactly what the shared composite cache used to do.
        Assert.Equal("01016B01000000", Hex(map, null));
        Assert.Equal("01016BBB", Hex(map, chainB));
        Assert.Equal("01016BAA", Hex(map, chainA));
    }

    [Fact]
    public void OverrideReachedThroughABcsStructFieldFollowsTheChain()
    {
        var holder = new Holder { Probe = new Probe { Value = 1 } };

        Assert.Equal("AA", Convert.ToHexString(BcsSerializer.Serialize(holder, ChainOverriding(0xAA))));
        Assert.Equal("01000000", Convert.ToHexString(BcsSerializer.Serialize(holder)));
    }

    [Fact]
    public void AChainStillCachesItsResolvedFormatter()
    {
        var chain = ChainOverriding(0xAA);

        Assert.Same(chain.GetFormatter<Dictionary<string, Probe>>(),
                    chain.GetFormatter<Dictionary<string, Probe>>());
    }

    [BcsStruct]
    public struct Probe
    {
        [BcsField(0)] public uint Value { get; set; }
    }

    [BcsStruct]
    public sealed class Holder
    {
        [BcsField(0)] public Probe Probe { get; set; }
    }

    private sealed class StubFormatter(byte marker) : IBcsFormatter<Probe>
    {
        public void Serialize(ref BcsWriter writer, Probe value) => writer.Write(marker);
        public Probe Deserialize(ref BcsReader reader) => new() { Value = reader.ReadBytesAsSpan(1)[0] };
    }
}
