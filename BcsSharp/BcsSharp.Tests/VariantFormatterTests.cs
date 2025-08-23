using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

// Test enum for variant formatter tests
[BcsEnum]
public interface ITestVariant
{
}

[BcsEnumVariant(0)]
public partial class TestVariantSimple : ITestVariant
{
    [BcsEnumData]
    public byte Value { get; set; }
}

[BcsEnumVariant(1)]
public partial class TestVariantComplex : ITestVariant
{
    [BcsEnumData]
    public string Name { get; set; } = "";
    
    [BcsEnumData]
    public uint Count { get; set; }
}

[BcsEnumVariant(2)]
public partial class TestVariantEmpty : ITestVariant
{
}

public class VariantFormatterTests
{
    [Fact]
    public void Should_Serialize_And_Deserialize_Interface_Types()
    {
        // Test interface serialization works correctly
        ITestVariant simple = new TestVariantSimple { Value = 42 };
        ITestVariant complex = new TestVariantComplex { Name = "test", Count = 123 };
        ITestVariant empty = new TestVariantEmpty();

        // Serialize through interface
        var serializedSimple = BcsSerializer.Serialize<ITestVariant>(simple);
        var serializedComplex = BcsSerializer.Serialize<ITestVariant>(complex);
        var serializedEmpty = BcsSerializer.Serialize<ITestVariant>(empty);

        // Verify correct format: variant index + data
        Assert.Equal(new byte[] { 0, 42 }, serializedSimple); // variant 0 + byte 42
        Assert.Equal(new byte[] { 1, 4, 116, 101, 115, 116, 123, 0, 0, 0 }, serializedComplex); // variant 1 + "test" + 123
        Assert.Equal(new byte[] { 2 }, serializedEmpty); // variant 2 only

        // Deserialize through interface
        var deserializedSimple = BcsSerializer.Deserialize<ITestVariant>(serializedSimple);
        var deserializedComplex = BcsSerializer.Deserialize<ITestVariant>(serializedComplex);
        var deserializedEmpty = BcsSerializer.Deserialize<ITestVariant>(serializedEmpty);

        // Verify results
        Assert.IsType<TestVariantSimple>(deserializedSimple);
        Assert.Equal(42, ((TestVariantSimple)deserializedSimple).Value);

        Assert.IsType<TestVariantComplex>(deserializedComplex);
        Assert.Equal("test", ((TestVariantComplex)deserializedComplex).Name);
        Assert.Equal(123u, ((TestVariantComplex)deserializedComplex).Count);

        Assert.IsType<TestVariantEmpty>(deserializedEmpty);
    }

    [Fact]
    public void Concrete_Variant_Types_Should_Not_Be_Serializable_Directly()
    {
        // Test that concrete variant types cannot be serialized directly
        // This matches Rust BCS behavior where enum variants are not separate serializable types
        var simple = new TestVariantSimple { Value = 42 };
        var complex = new TestVariantComplex { Name = "test", Count = 123 };
        var empty = new TestVariantEmpty();

        // These should fail because concrete variant types don't have formatters
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(simple));
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(complex));
        Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(empty));
    }
}