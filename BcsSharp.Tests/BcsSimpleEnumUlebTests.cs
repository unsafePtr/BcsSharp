using BcsSharp.Core;

namespace BcsSharp.Tests;

/// <summary>
/// BCS spec: enum variant indices are ULEB128-encoded.
/// For positions 0..127 that's a single byte (matches a raw u8).
/// For 128+ ULEB128 uses two or more bytes — at index 128 the encoding is [0x80, 0x01], not [0x80].
/// These tests guard against the single-byte regression in <c>BcsSimpleEnumFormatter</c>.
///
/// Ground-truth bytes come from the large_enum binary in Fixtures/rust-bcs; <c>RustParityTests.LargeEnumVariantIndex</c> checks them against the live fixture.
/// </summary>
public class BcsSimpleEnumUlebTests
{
    [Fact]
    public void EnumPosition0_SerializesAsSingleZeroByte()
    {
        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(LargeEnum.V0));
    }

    [Fact]
    public void EnumPosition127_SerializesAsSingleByte_7F()
    {
        Assert.Equal(new byte[] { 0x7F }, BcsSerializer.Serialize(LargeEnum.V127));
    }

    [Fact]
    public void EnumPosition128_SerializesAsULEB128_TwoBytes()
    {
        // From large_enum_128.bcs: V128 -> [0x80, 0x01]
        Assert.Equal(new byte[] { 0x80, 0x01 }, BcsSerializer.Serialize(LargeEnum.V128));
    }

    [Fact]
    public void EnumPosition129_SerializesAsULEB128_TwoBytes()
    {
        // From large_enum_129.bcs: V129 -> [0x81, 0x01]
        Assert.Equal(new byte[] { 0x81, 0x01 }, BcsSerializer.Serialize(LargeEnum.V129));
    }

    [Fact]
    public void EnumPositions_RoundTrip()
    {
        foreach (var v in new[] { LargeEnum.V0, LargeEnum.V127, LargeEnum.V128, LargeEnum.V129 })
        {
            var bytes = BcsSerializer.Serialize(v);
            var back = BcsSerializer.Deserialize<LargeEnum>(bytes);
            Assert.Equal(v, back);
        }
    }

    public enum LargeEnum
    {
        V0,
        V1,
        V2,
        V3,
        V4,
        V5,
        V6,
        V7,
        V8,
        V9,
        V10,
        V11,
        V12,
        V13,
        V14,
        V15,
        V16,
        V17,
        V18,
        V19,
        V20,
        V21,
        V22,
        V23,
        V24,
        V25,
        V26,
        V27,
        V28,
        V29,
        V30,
        V31,
        V32,
        V33,
        V34,
        V35,
        V36,
        V37,
        V38,
        V39,
        V40,
        V41,
        V42,
        V43,
        V44,
        V45,
        V46,
        V47,
        V48,
        V49,
        V50,
        V51,
        V52,
        V53,
        V54,
        V55,
        V56,
        V57,
        V58,
        V59,
        V60,
        V61,
        V62,
        V63,
        V64,
        V65,
        V66,
        V67,
        V68,
        V69,
        V70,
        V71,
        V72,
        V73,
        V74,
        V75,
        V76,
        V77,
        V78,
        V79,
        V80,
        V81,
        V82,
        V83,
        V84,
        V85,
        V86,
        V87,
        V88,
        V89,
        V90,
        V91,
        V92,
        V93,
        V94,
        V95,
        V96,
        V97,
        V98,
        V99,
        V100,
        V101,
        V102,
        V103,
        V104,
        V105,
        V106,
        V107,
        V108,
        V109,
        V110,
        V111,
        V112,
        V113,
        V114,
        V115,
        V116,
        V117,
        V118,
        V119,
        V120,
        V121,
        V122,
        V123,
        V124,
        V125,
        V126,
        V127,
        V128,
        V129,
    }
}
