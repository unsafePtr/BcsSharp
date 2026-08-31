using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Composite formatters capture their child formatters at construction and are cached globally, so a registration that is later undone must not outlive <see cref="BcsSerializer.ClearFormatterCache"/>.
/// </summary>
[Collection("GlobalFormatterRegistry")]
public class FormatterCacheClearTests
{
    [Fact]
    public void ClearFormatterCache_DropsMapFormatterBuiltFromAnUnregisteredChild()
    {
        var map = new Dictionary<string, CacheProbe> { ["a"] = new() { Value = 1 } };

        // First resolve of Dictionary<string, CacheProbe> happens while the override is
        // live, so the cached MapFormatter captures it as the value formatter.
        CustomFormatterResolver.Instance.Register<CacheProbe>(new ProbeOverrideFormatter());
        BcsSerializer.ClearFormatterCache();
        Assert.Equal("010161EE", Convert.ToHexString(BcsSerializer.Serialize(map)));

        CustomFormatterResolver.Instance.Unregister<CacheProbe>();
        BcsSerializer.ClearFormatterCache();

        // Rebuilt against the [BcsStruct] formatter: uint32 little-endian, not the 0xEE stub.
        Assert.Equal("01016101000000", Convert.ToHexString(BcsSerializer.Serialize(map)));
    }

    [BcsStruct]
    public struct CacheProbe
    {
        [BcsField(0)] public uint Value { get; set; }
    }

    private sealed class ProbeOverrideFormatter : IBcsFormatter<CacheProbe>
    {
        public void Serialize(ref BcsWriter writer, CacheProbe value) => writer.Write((byte)0xEE);
        public CacheProbe Deserialize(ref BcsReader reader) => new() { Value = reader.ReadBytesAsSpan(1)[0] };
    }
}
