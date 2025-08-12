using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using OneOf;
using OneOf.Types;

namespace BcsSharp.Tests;

[BcsStruct]
public struct TestSourceGeneratedStruct
{
    [BcsField(0)]
    public int Id { get; set; }

    [BcsField(1)]
    public string Name { get; set; }

    [BcsField(2)]
    public bool IsActive { get; set; }
}

[BcsStruct]
public class TestSourceGeneratedClass
{
    [BcsField(0)]
    public uint Count { get; set; }

    [BcsField(1)]
    public byte[] Data { get; set; } = [];
}

[BcsStruct]
public class TestAddress
{
    [BcsField(0)]
    public string Street { get; set; } = "";

    [BcsField(1)]
    public string City { get; set; } = "";
}

[BcsStruct]
public class TestPersonWithOptionalAddress
{
    [BcsField(0)]
    public string Name { get; set; } = "";

    [BcsField(1)]
    public OneOf<None, TestAddress> Address { get; set; }
}

[BcsStruct]
public class TestNullableFields
{
    [BcsField(0)]
    public string? NullableString { get; set; }

    [BcsField(1)]
    public int? NullableInt { get; set; }

    [BcsField(2)]
    public bool? NullableBool { get; set; }
}

[BcsStruct]
public class TestTupleFields
{
    [BcsField(0)]
    public (int, string) BasicTuple { get; set; }

    [BcsField(1)]
    public (uint, bool, string) ThreeTuple { get; set; }
}

public class SourceGeneratorTests
{
    [Fact]
    public void SourceGeneratedFormatter_Should_SerializeAndDeserialize_Struct()
    {
        var original = new TestSourceGeneratedStruct
        {
            Id = 42,
            Name = "Test",
            IsActive = true
        };

        var serialized = BcsSerializer.Serialize(original);
        var deserialized = BcsSerializer.Deserialize<TestSourceGeneratedStruct>(serialized);

        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.IsActive, deserialized.IsActive);
    }

    [Fact]
    public void SourceGeneratedFormatter_Should_SerializeAndDeserialize_Class()
    {
        var original = new TestSourceGeneratedClass
        {
            Count = 123,
            Data = [1, 2, 3, 4, 5]
        };

        var serialized = BcsSerializer.Serialize(original);
        var deserialized = BcsSerializer.Deserialize<TestSourceGeneratedClass>(serialized);

        Assert.Equal(original.Count, deserialized.Count);
        Assert.Equal(original.Data, deserialized.Data);
    }

    [Fact]
    public void SourceGeneratedFormatter_Should_SerializeAndDeserialize_OneOfNone()
    {
        // Test with None
        var personWithoutAddress = new TestPersonWithOptionalAddress
        {
            Name = "John",
            Address = new None()
        };

        var serialized1 = BcsSerializer.Serialize(personWithoutAddress);
        var deserialized1 = BcsSerializer.Deserialize<TestPersonWithOptionalAddress>(serialized1);

        Assert.Equal(personWithoutAddress.Name, deserialized1.Name);
        Assert.True(deserialized1.Address.IsT0); // Should be None

        // Test with Some(Address)
        var personWithAddress = new TestPersonWithOptionalAddress
        {
            Name = "Jane",
            Address = new TestAddress { Street = "123 Main St", City = "New York" }
        };

        var serialized2 = BcsSerializer.Serialize(personWithAddress);
        var deserialized2 = BcsSerializer.Deserialize<TestPersonWithOptionalAddress>(serialized2);

        Assert.Equal(personWithAddress.Name, deserialized2.Name);
        Assert.True(deserialized2.Address.IsT1); // Should be Some(Address)
        Assert.Equal("123 Main St", deserialized2.Address.AsT1.Street);
        Assert.Equal("New York", deserialized2.Address.AsT1.City);
    }

    //[Fact]
    //public void SourceGeneratedFormatter_Should_SerializeAndDeserialize_NullableFields()
    //{
    //    // Test with null values
    //    var nullFields = new TestNullableFields
    //    {
    //        NullableString = null,
    //        NullableInt = null,
    //        NullableBool = null
    //    };

    //    var serialized1 = BcsSerializer.Serialize(nullFields, BcsSharp.Generated.BcsSourceGeneratorResolver.Instance);
    //    var deserialized1 = BcsSerializer.Deserialize<TestNullableFields>(serialized1, BcsSharp.Generated.BcsSourceGeneratorResolver.Instance);

    //    Assert.Null(deserialized1.NullableString);
    //    Assert.Null(deserialized1.NullableInt);
    //    Assert.Null(deserialized1.NullableBool);

    //    // Test with actual values
    //    var valueFields = new TestNullableFields
    //    {
    //        NullableString = "Hello",
    //        NullableInt = 42,
    //        NullableBool = true
    //    };

    //    var serialized2 = BcsSerializer.Serialize(valueFields, BcsSharp.Generated.BcsSourceGeneratorResolver.Instance);
    //    var deserialized2 = BcsSerializer.Deserialize<TestNullableFields>(serialized2, BcsSharp.Generated.BcsSourceGeneratorResolver.Instance);

    //    Assert.Equal("Hello", deserialized2.NullableString);
    //    Assert.Equal(42, deserialized2.NullableInt);
    //    Assert.Equal(true, deserialized2.NullableBool);
    //}

    //[Fact]
    //public void SourceGeneratedFormatter_Should_SerializeAndDeserialize_TupleFields()
    //{
    //    var tupleData = new TestTupleFields
    //    {
    //        BasicTuple = (42, "Hello"),
    //        ThreeTuple = (123u, true, "World")
    //    };

    //    var serialized = BcsSerializer.Serialize(tupleData, BcsSharp.Generated.BcsSourceGeneratorResolver.Instance);
    //    var deserialized = BcsSerializer.Deserialize<TestTupleFields>(serialized, BcsSharp.Generated.BcsSourceGeneratorResolver.Instance);

    //    Assert.Equal(42, deserialized.BasicTuple.Item1);
    //    Assert.Equal("Hello", deserialized.BasicTuple.Item2);
    //    Assert.Equal(123u, deserialized.ThreeTuple.Item1);
    //    Assert.Equal(true, deserialized.ThreeTuple.Item2);
    //    Assert.Equal("World", deserialized.ThreeTuple.Item3);
    //}
}