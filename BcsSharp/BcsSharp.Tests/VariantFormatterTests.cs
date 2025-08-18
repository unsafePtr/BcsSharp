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
        // Test interface serialization still works
        ITestVariant simple = new TestVariantSimple { Value = 42 };
        ITestVariant complex = new TestVariantComplex { Name = "test", Count = 123 };
        ITestVariant empty = new TestVariantEmpty();

        // Serialize through interface
        var serializedSimple = BcsSerializer.Serialize<ITestVariant>(simple);
        var serializedComplex = BcsSerializer.Serialize<ITestVariant>(complex);
        var serializedEmpty = BcsSerializer.Serialize<ITestVariant>(empty);

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
    public void Should_Serialize_And_Deserialize_Concrete_Variant_Types()
    {
        // Test direct concrete type serialization - this is what we're adding support for
        var simple = new TestVariantSimple { Value = 42 };
        var complex = new TestVariantComplex { Name = "test", Count = 123 };
        var empty = new TestVariantEmpty();

        // Serialize concrete types directly
        var serializedSimple = BcsSerializer.Serialize(simple);
        var serializedComplex = BcsSerializer.Serialize(complex);
        var serializedEmpty = BcsSerializer.Serialize(empty);

        // Deserialize concrete types directly
        var deserializedSimple = BcsSerializer.Deserialize<TestVariantSimple>(serializedSimple);
        var deserializedComplex = BcsSerializer.Deserialize<TestVariantComplex>(serializedComplex);
        var deserializedEmpty = BcsSerializer.Deserialize<TestVariantEmpty>(serializedEmpty);

        // Verify results
        Assert.Equal(42, deserializedSimple.Value);
        Assert.Equal("test", deserializedComplex.Name);
        Assert.Equal(123u, deserializedComplex.Count);
        Assert.NotNull(deserializedEmpty);
    }

    [Fact]
    public void Concrete_And_Interface_Serialization_Should_Produce_Same_Result()
    {
        // Test that serializing concrete types vs interface types produces identical output
        var concreteVariant = new TestVariantSimple { Value = 42 };
        ITestVariant interfaceVariant = concreteVariant;

        var concreteSerialized = BcsSerializer.Serialize(concreteVariant);
        var interfaceSerialized = BcsSerializer.Serialize<ITestVariant>(interfaceVariant);

        // Both should produce identical bytes
        Assert.Equal(concreteSerialized, interfaceSerialized);

        // And both should deserialize to the same result
        var concreteDeserialized = BcsSerializer.Deserialize<TestVariantSimple>(concreteSerialized);
        var interfaceDeserialized = BcsSerializer.Deserialize<ITestVariant>(interfaceSerialized);

        Assert.Equal(concreteDeserialized.Value, ((TestVariantSimple)interfaceDeserialized).Value);
    }

    [Fact]
    public void Should_Handle_Cross_Deserialization()
    {
        // Test that data serialized as concrete type can be deserialized as interface and vice versa
        var original = new TestVariantComplex { Name = "cross-test", Count = 999 };

        // Serialize as concrete type
        var concreteSerialized = BcsSerializer.Serialize(original);

        // Deserialize as interface type
        var asInterface = BcsSerializer.Deserialize<ITestVariant>(concreteSerialized);
        Assert.IsType<TestVariantComplex>(asInterface);
        Assert.Equal("cross-test", ((TestVariantComplex)asInterface).Name);
        Assert.Equal(999u, ((TestVariantComplex)asInterface).Count);

        // Serialize as interface type
        ITestVariant interfaceRef = original;
        var interfaceSerialized = BcsSerializer.Serialize<ITestVariant>(interfaceRef);

        // Deserialize as concrete type
        var asConcrete = BcsSerializer.Deserialize<TestVariantComplex>(interfaceSerialized);
        Assert.Equal("cross-test", asConcrete.Name);
        Assert.Equal(999u, asConcrete.Count);
    }
}