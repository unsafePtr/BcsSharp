using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Generated;
using OneOf;
using OneOf.Types;

namespace BcsSharp.Tests;

/// <summary>
/// Comprehensive test class to verify source generator functionality and diagnose issues
/// </summary>
public class SourceGeneratorDiagnosticsTest
{
    [Fact]
    public void SourceGenerator_Should_ProduceWorkingFormatters()
    {
        // Test basic struct
        var testStruct = new TestSourceGeneratedStruct
        {
            Id = 42,
            Name = "Test",
            IsActive = true
        };

        // Serialize using generated formatter directly
        var writer = new BcsWriter();
        BcsSharp.Tests.Generated.BcsSharp_Tests_TestSourceGeneratedStructFormatter.Instance.Serialize(ref writer, testStruct);

        // Deserialize using generated formatter directly
        var serializedData = writer.ToBytes();
        var reader = new BcsReader(serializedData);
        var deserialized = BcsSharp.Tests.Generated.BcsSharp_Tests_TestSourceGeneratedStructFormatter.Instance.Deserialize(ref reader);

        Assert.Equal(testStruct.Id, deserialized.Id);
        Assert.Equal(testStruct.Name, deserialized.Name);
        Assert.Equal(testStruct.IsActive, deserialized.IsActive);
    }

    [Fact]
    public void SourceGeneratorResolver_Should_ResolveFormatters()
    {
        var resolver = BcsSourceGeneratorResolver.Instance;

        // Test that resolver can find the generated formatter
        var formatter = resolver.GetFormatter<TestSourceGeneratedStruct>();
        Assert.NotNull(formatter);

        // Test that resolver returns the same instance
        var formatter2 = resolver.GetFormatter<TestSourceGeneratedStruct>();
        Assert.Same(formatter, formatter2);
    }

    [Fact]
    public void SourceGenerator_Should_HandleComplexTypes()
    {
        var testClass = new TestSourceGeneratedClass
        {
            Count = 123,
            Data = [1, 2, 3, 4, 5]
        };

        // Use resolver to get formatter
        var resolver = BcsSourceGeneratorResolver.Instance;
        var formatter = resolver.GetFormatter<TestSourceGeneratedClass>();
        Assert.NotNull(formatter);

        // Test serialization/deserialization
        var writer = new BcsWriter();
        formatter.Serialize(ref writer, testClass);

        var serializedData = writer.ToBytes();
        var reader = new BcsReader(serializedData);
        var deserialized = formatter.Deserialize(ref reader);

        Assert.Equal(testClass.Count, deserialized.Count);
        Assert.Equal(testClass.Data, deserialized.Data);
    }

    [Fact]
    public void SourceGenerator_Should_HandleOneOfTypes()
    {
        // Test with None
        var personWithoutAddress = new TestPersonWithOptionalAddress
        {
            Name = "John",
            Address = new None()
        };

        var formatter = BcsSourceGeneratorResolver.Instance.GetFormatter<TestPersonWithOptionalAddress>();
        Assert.NotNull(formatter);

        var writer = new BcsWriter();
        formatter.Serialize(ref writer, personWithoutAddress);

        var serializedData = writer.ToBytes();
        var reader = new BcsReader(serializedData);
        var deserialized = formatter.Deserialize(ref reader);

        Assert.Equal(personWithoutAddress.Name, deserialized.Name);
        Assert.True(deserialized.Address.IsT0); // Should be None

        // Test with Some(Address)
        var personWithAddress = new TestPersonWithOptionalAddress
        {
            Name = "Jane",
            Address = new TestAddress { Street = "123 Main St", City = "New York" }
        };

        writer = new BcsWriter();
        formatter.Serialize(ref writer, personWithAddress);

        serializedData = writer.ToBytes();
        reader = new BcsReader(serializedData);
        deserialized = formatter.Deserialize(ref reader);

        Assert.Equal(personWithAddress.Name, deserialized.Name);
        Assert.True(deserialized.Address.IsT1); // Should be Some(Address)
        Assert.Equal("123 Main St", deserialized.Address.AsT1.Street);
        Assert.Equal("New York", deserialized.Address.AsT1.City);
    }

    [Fact]
    public void SourceGenerator_Should_IntegrateWithBcsSerializer()
    {
        var testStruct = new TestSourceGeneratedStruct
        {
            Id = 42,
            Name = "Integration Test",
            IsActive = true
        };

        // Serialize using BcsSerializer (should use generated formatter via resolver)
        var serialized = BcsSerializer.Serialize(testStruct, BcsSourceGeneratorResolver.Instance);
        var deserialized = BcsSerializer.Deserialize<TestSourceGeneratedStruct>(serialized, BcsSourceGeneratorResolver.Instance);

        Assert.Equal(testStruct.Id, deserialized.Id);
        Assert.Equal(testStruct.Name, deserialized.Name);
        Assert.Equal(testStruct.IsActive, deserialized.IsActive);
    }

    [Fact]
    public void SourceGenerator_Should_HandleNestedBcsStructTypes()
    {
        var address = new TestAddress
        {
            Street = "123 Main St",
            City = "Test City"
        };

        var personWithAddress = new TestPersonWithOptionalAddress
        {
            Name = "Test Person",
            Address = address
        };

        var resolver = BcsSourceGeneratorResolver.Instance;

        // Both types should have generated formatters
        var addressFormatter = resolver.GetFormatter<TestAddress>();
        var personFormatter = resolver.GetFormatter<TestPersonWithOptionalAddress>();

        Assert.NotNull(addressFormatter);
        Assert.NotNull(personFormatter);

        // Test full serialization/deserialization
        var writer = new BcsWriter();
        personFormatter.Serialize(ref writer, personWithAddress);

        var serializedData = writer.ToBytes();
        var reader = new BcsReader(serializedData);
        var deserialized = personFormatter.Deserialize(ref reader);

        Assert.Equal(personWithAddress.Name, deserialized.Name);
        Assert.True(deserialized.Address.IsT1);
        Assert.Equal(address.Street, deserialized.Address.AsT1.Street);
        Assert.Equal(address.City, deserialized.Address.AsT1.City);
    }

    [Fact]
    public void SourceGenerator_ResolverShouldBeListedInGeneratedFiles()
    {
        // This test verifies that the resolver is properly generated
        // by checking that it can be instantiated and accessed
        var resolver = BcsSourceGeneratorResolver.Instance;
        Assert.NotNull(resolver);

        // Check that it implements IFormatterResolver
        Assert.IsAssignableFrom<IFormatterResolver>(resolver);

        // Check that it can resolve known types
        Assert.NotNull(resolver.GetFormatter<TestSourceGeneratedStruct>());
        Assert.NotNull(resolver.GetFormatter<TestSourceGeneratedClass>());
        Assert.NotNull(resolver.GetFormatter<TestAddress>());
        Assert.NotNull(resolver.GetFormatter<TestPersonWithOptionalAddress>());

        // Check that it returns null for unknown types
        Assert.Null(resolver.GetFormatter<string>());
        Assert.Null(resolver.GetFormatter<int>());
    }

    [Fact]
    public void SourceGenerator_ShouldWork_WithBcsSerializerDirectly()
    {
        var testData = new TestSourceGeneratedStruct
        {
            Id = 999,
            Name = "Direct Test",
            IsActive = false
        };

        // This should use the generated formatter if it's available
        var serialized = BcsSerializer.Serialize(testData);
        var deserialized = BcsSerializer.Deserialize<TestSourceGeneratedStruct>(serialized);

        Assert.Equal(testData.Id, deserialized.Id);
        Assert.Equal(testData.Name, deserialized.Name);
        Assert.Equal(testData.IsActive, deserialized.IsActive);
    }
}