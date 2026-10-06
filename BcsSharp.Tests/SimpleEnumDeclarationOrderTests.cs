using BcsSharp.Core;

namespace BcsSharp.Tests;

/// <summary>
/// A CLR enum encodes the declaration index of its member, as serde does for a unit-only Rust enum, whatever value the member was assigned.
/// <c>Enum.GetValues</c> sorts by value, so an enum whose values were not ascending used to serialize the wrong index.
/// Ground-truth bytes come from the descending_enum fixtures in Fixtures/rust-bcs; <c>RustParityTests.NonAscendingEnumVariantIndex</c> checks them against the live fixture.
/// </summary>
public class SimpleEnumDeclarationOrderTests
{
    [Fact]
    public void NonAscendingValues_EncodeTheDeclarationIndex()
    {
        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(Descending.First));
        Assert.Equal(new byte[] { 0x01 }, BcsSerializer.Serialize(Descending.Second));
        Assert.Equal(new byte[] { 0x02 }, BcsSerializer.Serialize(Descending.Third));
    }

    [Fact]
    public void NegativeValue_EncodesTheDeclarationIndex()
    {
        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(Signed.Minus));
        Assert.Equal(new byte[] { 0x01 }, BcsSerializer.Serialize(Signed.Zero));
    }

    [Fact]
    public void DeclarationIndex_RoundTrips()
    {
        foreach (var value in new[] { Descending.First, Descending.Second, Descending.Third })
        {
            Assert.Equal(value, BcsSerializer.Deserialize<Descending>(BcsSerializer.Serialize(value)));
        }

        Assert.Equal(Signed.Minus, BcsSerializer.Deserialize<Signed>(new byte[] { 0x00 }));
        Assert.Equal(Signed.Zero, BcsSerializer.Deserialize<Signed>(new byte[] { 0x01 }));
    }

    [Fact]
    public void AliasedMembers_AreRejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(Aliased.A));

        Assert.Equal("Enum Aliased declares A and B with the same value, which BCS cannot encode as two variants.", ex.Message);
    }

    public enum Descending { First = 30, Second = 20, Third = 10 }

    public enum Signed { Minus = -1, Zero = 0 }

    public enum Aliased { A = 1, B = 1 }
}
