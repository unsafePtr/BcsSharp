using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Resolvers;
using Nethermind.Int256;

namespace BcsSharp.Tests.Extensibility;

/// <summary>
/// Proves the custom-formatter extension point carries 256-bit integers end to end now that
/// BcsSharp.Core no longer ships them: registration happens once in
/// <see cref="SuiFormatterRegistration"/>, and every composition below resolves through the
/// same chain a consuming application would use.
/// </summary>
public class SuiUInt256FormatterTests
{
    [Fact]
    public void Core_DoesNotResolve_UInt256_Natively()
    {
        // Pins the decision to drop 256-bit support from the library: the built-in resolver
        // must not know the type. If someone re-adds it to StandardResolver, this fails and
        // the Nethermind dependency has silently crept back into the shipping package.
        Assert.Null(StandardResolver.Instance.GetFormatter<UInt256>());

        // ...while the full chain resolves it, because the test assembly registered a formatter.
        Assert.NotNull(CompositeResolver.Default.GetFormatter<UInt256>());
    }

    [Fact]
    public void Serialize_Is32BareLittleEndianBytes()
    {
        var serialized = BcsSerializer.Serialize(new UInt256(0x12345678));

        Assert.Equal(32, serialized.Length);
        Assert.Equal(0x78, serialized[0]);
        Assert.Equal(0x56, serialized[1]);
        Assert.Equal(0x34, serialized[2]);
        Assert.Equal(0x12, serialized[3]);

        for (var i = 4; i < 32; i++)
        {
            Assert.Equal(0x00, serialized[i]);
        }
    }

    [Fact]
    public void Serialize_MaxValue_IsThirtyTwo0xFFBytes()
    {
        var serialized = BcsSerializer.Serialize(UInt256.MaxValue);

        Assert.Equal(Enumerable.Repeat((byte)0xFF, 32), serialized);
    }

    [Fact]
    public void Serialize_Zero_IsThirtyTwoZeroBytes()
    {
        var serialized = BcsSerializer.Serialize(UInt256.Zero);

        Assert.Equal(new byte[32], serialized);
    }

    [Fact]
    public void Serialize_HasNoLengthPrefix_UnlikeByteSequence()
    {
        // The whole point of the Sui convention: u256 rides on BCS's *fixed-size* array
        // encoding (Rust `[u8; 32]` -> serialize_tuple), so there is no length prefix.
        // The same 32 bytes as a variable-length sequence (Rust `Vec<u8>`) cost one more
        // byte: ULEB128 0x20 followed by the data.
        var asU256 = BcsSerializer.Serialize(UInt256.MaxValue);
        var asSequence = BcsSerializer.Serialize(Enumerable.Repeat((byte)0xFF, 32).ToList());

        Assert.Equal(32, asU256.Length);
        Assert.Equal(33, asSequence.Length);
        Assert.Equal(0x20, asSequence[0]);
        Assert.Equal(asU256, asSequence[1..]);
    }

    [Theory]
    [MemberData(nameof(RoundTripValues))]
    public void RoundTrip_PreservesValue(UInt256 value)
    {
        var serialized = BcsSerializer.Serialize(value);

        Assert.Equal(value, BcsSerializer.Deserialize<UInt256>(serialized));
    }

    public static TheoryData<UInt256> RoundTripValues() =>
    [
        UInt256.Zero,
        UInt256.One,
        new UInt256(0x12345678),
        new UInt256(ulong.MaxValue),
        UInt256.MaxValue,
    ];

    [Fact]
    public void Option_None_IsSingleZeroByte()
    {
        UInt256? none = null;

        var serialized = BcsSerializer.Serialize(none);

        Assert.Equal([0x00], serialized);
        Assert.Null(BcsSerializer.Deserialize<UInt256?>(serialized));
    }

    [Fact]
    public void Option_Some_IsTagByteThen32BareBytes()
    {
        UInt256? some = UInt256.MaxValue;

        var serialized = BcsSerializer.Serialize(some);

        Assert.Equal(33, serialized.Length);
        Assert.Equal(0x01, serialized[0]);
        Assert.Equal(Enumerable.Repeat((byte)0xFF, 32), serialized[1..]);
        Assert.Equal(some, BcsSerializer.Deserialize<UInt256?>(serialized));
    }

    [Fact]
    public void List_IsUlebCountThenBareElements()
    {
        var values = new List<UInt256> { UInt256.Zero, UInt256.MaxValue };

        var serialized = BcsSerializer.Serialize(values);

        Assert.Equal(1 + 64, serialized.Length);
        Assert.Equal(0x02, serialized[0]);
        Assert.Equal(new byte[32], serialized[1..33]);
        Assert.Equal(Enumerable.Repeat((byte)0xFF, 32), serialized[33..]);
        Assert.Equal(values, BcsSerializer.Deserialize<List<UInt256>>(serialized));
    }

    [Fact]
    public void StructField_ResolvesThroughCustomChain()
    {
        var wallet = new Wallet { Balance = new UInt256(0x12345678), IsActive = true };

        var serialized = BcsSerializer.Serialize(wallet);

        Assert.Equal(33, serialized.Length);
        Assert.Equal([0x78, 0x56, 0x34, 0x12], serialized[..4]);
        Assert.Equal(0x01, serialized[^1]);

        var back = BcsSerializer.Deserialize<Wallet>(serialized);
        Assert.Equal(wallet.Balance, back.Balance);
        Assert.Equal(wallet.IsActive, back.IsActive);
    }

    [BcsStruct]
    public class Wallet
    {
        [BcsField(0)]
        public UInt256 Balance { get; set; }
        [BcsField(1)]
        public bool IsActive { get; set; }
    }
}
