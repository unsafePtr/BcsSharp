using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using Xunit;

namespace BcsSharp.Tests;

public class NestedEnumVariantTests
{
    #region Outer Enum Definition
    
    [BcsEnum]
    public interface IOuterEnum { }
    
    [BcsEnumVariant(0)]
    public class OuterSimple : IOuterEnum
    {
        [BcsEnumData(0)]
        public string Message { get; set; } = "";
        
        public OuterSimple() { }
        public OuterSimple(string message) { Message = message; }
    }
    
    [BcsEnumVariant(1)]
    public class OuterWithInner : IOuterEnum
    {
        [BcsEnumData(0)]
        public IInnerEnum InnerValue { get; set; } = new InnerA();
        
        public OuterWithInner() { }
        public OuterWithInner(IInnerEnum innerValue) { InnerValue = innerValue; }
    }
    
    #endregion
    
    #region Inner Enum Definition
    
    [BcsEnum]
    public interface IInnerEnum { }
    
    [BcsEnumVariant(0)]
    public class InnerA : IInnerEnum
    {
        [BcsEnumData(0)]
        public uint Number { get; set; }
        
        public InnerA() { }
        public InnerA(uint number) { Number = number; }
    }
    
    [BcsEnumVariant(1)]
    public class InnerB : IInnerEnum
    {
        [BcsEnumData(0)]
        public string Text { get; set; } = "";
        
        public InnerB() { }
        public InnerB(string text) { Text = text; }
    }
    
    [BcsEnumVariant(2)]
    public class InnerUnit : IInnerEnum
    {
        public InnerUnit() { }
    }
    
    #endregion
    
    [Fact]
    public void NestedEnumVariant_WithInnerA_ShouldSerializeCorrectly()
    {
        // Arrange - Create outer variant containing InnerA
        var innerA = new InnerA(42);
        var outer = new OuterWithInner(innerA);
        
        // Act
        var serialized = BcsSerializer.Serialize<IOuterEnum>(outer);
        var deserialized = BcsSerializer.Deserialize<IOuterEnum>(serialized);
        
        // Assert structure
        Assert.True(serialized.Length >= 3); // At least: outer index + inner index + uint value
        Assert.Equal(1, serialized[0]); // OuterWithInner variant index
        Assert.Equal(0, serialized[1]); // InnerA variant index
        
        // Assert deserialization
        Assert.IsType<OuterWithInner>(deserialized);
        var deserializedOuter = (OuterWithInner)deserialized;
        Assert.IsType<InnerA>(deserializedOuter.InnerValue);
        var deserializedInner = (InnerA)deserializedOuter.InnerValue;
        Assert.Equal(42u, deserializedInner.Number);
    }
    
    [Fact]
    public void NestedEnumVariant_WithInnerB_ShouldSerializeCorrectly()
    {
        // Arrange - Create outer variant containing InnerB
        var innerB = new InnerB("nested test");
        var outer = new OuterWithInner(innerB);
        
        // Act
        var serialized = BcsSerializer.Serialize<IOuterEnum>(outer);
        var deserialized = BcsSerializer.Deserialize<IOuterEnum>(serialized);
        
        // Assert structure
        Assert.True(serialized.Length >= 3); // At least: outer index + inner index + string length
        Assert.Equal(1, serialized[0]); // OuterWithInner variant index
        Assert.Equal(1, serialized[1]); // InnerB variant index
        
        // Assert deserialization
        Assert.IsType<OuterWithInner>(deserialized);
        var deserializedOuter = (OuterWithInner)deserialized;
        Assert.IsType<InnerB>(deserializedOuter.InnerValue);
        var deserializedInner = (InnerB)deserializedOuter.InnerValue;
        Assert.Equal("nested test", deserializedInner.Text);
    }
    
    [Fact]
    public void NestedEnumVariant_WithInnerUnit_ShouldSerializeCorrectly()
    {
        // Arrange - Create outer variant containing unit variant
        var innerUnit = new InnerUnit();
        var outer = new OuterWithInner(innerUnit);
        
        // Act
        var serialized = BcsSerializer.Serialize<IOuterEnum>(outer);
        var deserialized = BcsSerializer.Deserialize<IOuterEnum>(serialized);
        
        // Assert structure
        Assert.Equal(2, serialized.Length); // outer index + inner unit index
        Assert.Equal(1, serialized[0]); // OuterWithInner variant index
        Assert.Equal(2, serialized[1]); // InnerUnit variant index
        
        // Assert deserialization
        Assert.IsType<OuterWithInner>(deserialized);
        var deserializedOuter = (OuterWithInner)deserialized;
        Assert.IsType<InnerUnit>(deserializedOuter.InnerValue);
    }
    
    [Fact]
    public void NestedEnumVariant_SerializationFormat_ShouldBeCorrect()
    {
        // Test to verify exact serialization format
        var inner = new InnerA(8000); // Same as in BcsEnumSerializationTests
        var outer = new OuterWithInner(inner);
        
        var serialized = BcsSerializer.Serialize<IOuterEnum>(outer);
        
        // Let's check what we actually get
        var actualBytes = string.Join(", ", serialized.Select(b => $"0x{b:X2}"));
        
        // Expected format:
        // [1] - OuterWithInner variant index
        // [0] - InnerA variant index  
        // [0x40, 0x1F, 0x00, 0x00] - 8000 as u32 (uint) little-endian
        Assert.Equal(6, serialized.Length);
        Assert.Equal(1, serialized[0]);    // Outer variant index
        Assert.Equal(0, serialized[1]);    // Inner variant index
        Assert.Equal(0x40, serialized[2]); // 8000 low byte
        Assert.Equal(0x1F, serialized[3]); // 8000 second byte
        Assert.Equal(0x00, serialized[4]); // 8000 third byte  
        Assert.Equal(0x00, serialized[5]); // 8000 high byte
    }
    
    [Fact]
    public void NestedEnumVariant_DeeplyNested_ShouldWork()
    {
        // Create a more complex nested structure
        var complexInner = new InnerB("deeply nested data");
        var outer = new OuterWithInner(complexInner);
        
        // Serialize the outer simple variant for comparison
        var simpleOuter = new OuterSimple("simple message");
        var simpleSerialized = BcsSerializer.Serialize<IOuterEnum>(simpleOuter);
        
        // Serialize the nested variant
        var nestedSerialized = BcsSerializer.Serialize<IOuterEnum>(outer);
        
        // They should have different variant indices
        Assert.NotEqual(simpleSerialized[0], nestedSerialized[0]);
        
        // Deserialize and verify
        var deserializedSimple = BcsSerializer.Deserialize<IOuterEnum>(simpleSerialized);
        var deserializedNested = BcsSerializer.Deserialize<IOuterEnum>(nestedSerialized);
        
        Assert.IsType<OuterSimple>(deserializedSimple);
        Assert.IsType<OuterWithInner>(deserializedNested);
        
        var outerNested = (OuterWithInner)deserializedNested;
        Assert.IsType<InnerB>(outerNested.InnerValue);
        Assert.Equal("deeply nested data", ((InnerB)outerNested.InnerValue).Text);
    }
}