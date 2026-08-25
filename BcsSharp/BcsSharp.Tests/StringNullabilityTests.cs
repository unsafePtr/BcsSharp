using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// BCS has no null string: the optional form is <c>Option&lt;String&gt;</c>. These tests pin
/// that a null is rejected rather than coerced to empty, and that a <c>string?</c> field is
/// therefore not a substitute for <c>Option&lt;string&gt;</c> — the fixture type in
/// Fixtures/rust-bcs/src/types.rs declares <c>pub email: Option&lt;String&gt;</c>, which
/// encodes with a discriminant that a plain string does not carry.
/// </summary>
public class StringNullabilityTests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

    [Fact]
    public void NullStringIsRejectedRatherThanEncodedAsEmpty()
    {
        // Coercing null to "" would emit 0x00 — identical to Option.None — which is exactly
        // how a mis-modelled `string?` field used to masquerade as an optional.
        Assert.Throws<ArgumentNullException>(() => BcsSerializer.Serialize<string>(null!));
    }

    [Fact]
    public void EmptyStringIsStillValidAndEncodesAsZeroLength()
    {
        var bytes = BcsSerializer.Serialize(string.Empty);

        Assert.Equal("00", Hex(bytes));
        Assert.Equal(string.Empty, BcsSerializer.Deserialize<string>(bytes));
    }

    [Fact]
    public void NullStringFieldOnAStructIsRejected()
    {
        var holder = new NullableStringHolder { Text = null };

        // Surfaces the modelling error where it is made, instead of at a wire mismatch with
        // Rust much later. The fix is to declare the field as Option<string>.
        Assert.Throws<ArgumentNullException>(() => BcsSerializer.Serialize(holder));
    }

    [Fact]
    public void AStringFieldEncodesWithoutTheOptionDiscriminant()
    {
        var holder = new NullableStringHolder { Text = "hi" };
        Option<string> option = "hi";

        // Plain string: ULEB length + UTF-8.
        Assert.Equal("026869", Hex(BcsSerializer.Serialize(holder)));
        // Option<string>: tag 1 + ULEB length + UTF-8. Rust encodes Option<String> this way,
        // so the two are not interchangeable even though both are "a string that may be absent".
        Assert.Equal("01026869", Hex(BcsSerializer.Serialize(option)));
    }

    [Fact]
    public void OptionNoneEncodesAsASingleZeroTag()
    {
        Option<string> none = None.Instance;

        Assert.Equal("00", Hex(BcsSerializer.Serialize(none)));
    }

    [BcsStruct]
    public sealed class NullableStringHolder
    {
        [BcsField(0)] public string? Text { get; set; }
    }
}
