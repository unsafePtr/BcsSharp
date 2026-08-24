using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// Pins how a nullable string behaves on the wire today. Both facts below are divergences
/// from the Rust reference, kept as tests so they are visible rather than latent:
/// the fixture type in Fixtures/rust-bcs/src/types.rs declares <c>pub email: Option&lt;String&gt;</c>,
/// while the C# model declares <c>string? Email</c> — and those do not encode the same way.
/// </summary>
public class StringNullabilityTests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

    [Fact]
    public void NullAndEmptyStringAreIndistinguishableOnTheWire()
    {
        // BcsWriter.WriteString collapses both to ULEB 0, so the distinction is destroyed.
        Assert.Equal("00", Hex(BcsSerializer.Serialize<string>(null!)));
        Assert.Equal("00", Hex(BcsSerializer.Serialize(string.Empty)));
    }

    [Fact]
    public void NullStringRoundTripsBackAsEmptyNotNull()
    {
        var bytes = BcsSerializer.Serialize<string>(null!);

        var back = BcsSerializer.Deserialize<string>(bytes);

        // ReadString decodes a zero-length span, which yields "" — it can never return null,
        // so a null written into a string field does not survive the round trip.
        Assert.NotNull(back);
        Assert.Equal(string.Empty, back);
    }

    [Fact]
    public void NullableStringFieldEncodesAsPlainStringNotAsOption()
    {
        var holder = new NullableStringHolder { Text = "hi" };
        Option<string> option = "hi";

        var fieldBytes = Hex(BcsSerializer.Serialize(holder));
        var optionBytes = Hex(BcsSerializer.Serialize(option));

        // A `string?` field resolves to StringFormatter: ULEB length + UTF-8, no tag.
        Assert.Equal("026869", fieldBytes);
        // Option<string> is a union: tag 1 + ULEB length + UTF-8.
        Assert.Equal("01026869", optionBytes);

        // Rust encodes Option<String> the second way, so a populated `string?` field is a
        // silent wire mismatch — it is missing the 0x01 discriminant.
        Assert.NotEqual(optionBytes, fieldBytes);
    }

    [Fact]
    public void NullableStringFieldAndOptionNoneCoincideOnlyWhenAbsent()
    {
        var holder = new NullableStringHolder { Text = null };
        Option<string> none = None.Instance;

        // Both are a single 0x00 — ULEB 0 for the empty string, tag 0 for None. This
        // coincidence is why the Rust parity fixture (which only ever sets email to None)
        // passes despite the mismatch above.
        Assert.Equal("00", Hex(BcsSerializer.Serialize(holder)));
        Assert.Equal("00", Hex(BcsSerializer.Serialize(none)));
    }

    [BcsStruct]
    public sealed class NullableStringHolder
    {
        [BcsField(0)] public string? Text { get; set; }
    }
}
