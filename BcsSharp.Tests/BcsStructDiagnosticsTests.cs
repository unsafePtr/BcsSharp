using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// A misconfigured <c>[BcsStruct]</c> must fail with the message that names the mistake.
/// <c>ObjectResolver</c> used to turn every construction error into a null formatter, so each of these surfaced as "No formatter found for type X" with the cause gone.
/// </summary>
public class BcsStructDiagnosticsTests
{
    [Fact]
    public void ArrayField_NamesTheArrayRule()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(new WithArray()));

        Assert.Equal("It's not allowed to use generic arrays in BCS serialization unless it's a custom type with dedicated formatter. Use List<T> instead", ex.Message);
    }

    [Fact]
    public void ReadOnlyProperty_NamesTheProperty()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(new WithReadOnlyProperty()));

        Assert.Equal("Property Value in WithReadOnlyProperty must be writable", ex.Message);
    }

    [Fact]
    public void MissingParameterlessConstructor_NamesTheType()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(new NoParameterlessCtor(5)));

        Assert.Equal("Type NoParameterlessCtor must have a parameterless constructor for BCS deserialization", ex.Message);
    }

    [Fact]
    public void UnresolvableFieldType_NamesTheField()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(new WithUnresolvable()));

        Assert.Equal("No BCS formatter found for field Value of type Unresolvable", ex.Message);
    }

    [Fact]
    public void NoBcsFields_NamesTheType()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<WithoutFields>([]));

        Assert.Equal("Type WithoutFields has no serializable fields marked with [BcsField]", ex.Message);
    }

    [Fact]
    public void DuplicateFieldOrder_NamesBothMembers()
    {
        // Two members with one order would leave the wire order to the sort.
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(new DuplicateOrder()));

        Assert.Equal("DuplicateOrder declares [BcsField(0)] on both A and B.", ex.Message);
    }

    [Fact]
    public void GappedFieldOrders_NameTheExpectedRange()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(new GappedOrder()));

        Assert.Equal("GappedOrder must number its [BcsField] members 0..1 without gaps; found 0, 5.", ex.Message);
    }

    [BcsStruct]
    public sealed class WithArray
    {
        [BcsField(0)] public uint[] Data { get; set; } = [];
    }

    [BcsStruct]
    public sealed class WithReadOnlyProperty
    {
        [BcsField(0)] public uint Value { get; } = 7;
    }

    [BcsStruct]
    public sealed class NoParameterlessCtor(uint value)
    {
        [BcsField(0)] public uint Value { get; set; } = value;
    }

    public sealed class Unresolvable;

    [BcsStruct]
    public sealed class WithUnresolvable
    {
        [BcsField(0)] public Unresolvable Value { get; set; } = new();
    }

    [BcsStruct]
    public sealed class WithoutFields
    {
        public uint Value { get; set; }
    }

    [BcsStruct]
    public sealed class DuplicateOrder
    {
        [BcsField(0)] public uint A { get; set; }
        [BcsField(0)] public uint B { get; set; }
    }

    [BcsStruct]
    public sealed class GappedOrder
    {
        [BcsField(0)] public uint A { get; set; }
        [BcsField(5)] public uint B { get; set; }
    }
}
