using System;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using Xunit;

namespace BcsSharp.Tests;

/// <summary>
/// Tests for proper BCS enum serialization following the official BCS specification.
/// BCS enums use ULEB128-encoded variant indices and support associated data of any BCS type.
/// 
/// Format: [ULEB128 variant_index] + [associated_data...]
/// Example: E::Variant2("hello") -> [2, 5, 'h', 'e', 'l', 'l', 'o']
/// </summary>
public class BcsEnumSerializationTests
{
    #region Test Enum Definitions (following BCS specification)

    /// <summary>
    /// Base interface for the test enum (similar to Rust enum)
    /// enum E { Variant0(u16), Variant1(u8), Variant2(String) }
    /// </summary>
    [BcsEnum]
    public interface ITestEnum { }

    /// <summary>
    /// Variant 0 with u16 data - maps to E::Variant0(u16)
    /// </summary>
    [BcsEnumVariant(0)]
    public class Variant0 : ITestEnum
    {
        [BcsEnumData(0)]
        public ushort Value { get; set; }

        public Variant0() { }
        public Variant0(ushort value) { Value = value; }
    }

    /// <summary>
    /// Variant 1 with u8 data - maps to E::Variant1(u8)
    /// </summary>
    [BcsEnumVariant(1)]
    public class Variant1 : ITestEnum
    {
        [BcsEnumData(0)]
        public byte Value { get; set; }

        public Variant1() { }
        public Variant1(byte value) { Value = value; }
    }

    /// <summary>
    /// Variant 2 with String data - maps to E::Variant2(String)
    /// </summary>
    [BcsEnumVariant(2)]
    public class Variant2 : ITestEnum
    {
        [BcsEnumData(0)]
        public string Value { get; set; } = "";

        public Variant2() { }
        public Variant2(string value) { Value = value; }
    }

    /// <summary>
    /// Unit variant (no associated data) - maps to E::Unit
    /// </summary>
    [BcsEnumVariant(3)]
    public class UnitVariant : ITestEnum
    {
        public UnitVariant() { }
    }

    /// <summary>
    /// Complex variant with multiple fields - maps to E::Complex(u32, String, bool)
    /// </summary>
    [BcsEnumVariant(4)]
    public class ComplexVariant : ITestEnum
    {
        [BcsEnumData(0)]
        public uint Number { get; set; }

        [BcsEnumData(1)]
        public string Text { get; set; } = "";

        [BcsEnumData(2)]
        public bool Flag { get; set; }

        public ComplexVariant() { }
        public ComplexVariant(uint number, string text, bool flag)
        {
            Number = number;
            Text = text;
            Flag = flag;
        }
    }

    /// <summary>
    /// Optional-like enum similar to Rust Option<T>
    /// </summary>
    [BcsEnum]
    public interface IOption<T> { }

    [BcsEnumVariant(0)]
    public class None<T> : IOption<T>
    {
        public None() { }
    }

    [BcsEnumVariant(1)]
    public class Some<T> : IOption<T>
    {
        [BcsEnumData(0)]
        public T Value { get; set; } = default!;

        public Some() { }
        public Some(T value) { Value = value; }
    }

    #endregion

    [Fact]
    public void BcsEnum_Variant0_ShouldMatchBcsSpecification()
    {
        // Arrange - E::Variant0(8000) should be [0, 0x40, 0x1F]
        var variant = new Variant0(8000);

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);

        // Assert - Should match BCS specification exactly
        Assert.Equal(3, serialized.Length);
        Assert.Equal(0, serialized[0]); // Variant index 0 as ULEB128
        Assert.Equal(0x40, serialized[1]); // 8000 as u16 little-endian (low byte)
        Assert.Equal(0x1F, serialized[2]); // 8000 as u16 little-endian (high byte)

        // Round-trip test
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);
        Assert.IsType<Variant0>(deserialized);
        Assert.Equal(8000, ((Variant0)deserialized).Value);
    }

    [Fact]
    public void BcsEnum_Variant1_ShouldMatchBcsSpecification()
    {
        // Arrange - E::Variant1(255) should be [1, 0xFF]
        var variant = new Variant1(255);

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);

        // Assert - Should match BCS specification exactly
        Assert.Equal(2, serialized.Length);
        Assert.Equal(1, serialized[0]); // Variant index 1 as ULEB128
        Assert.Equal(0xFF, serialized[1]); // 255 as u8

        // Round-trip test
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);
        Assert.IsType<Variant1>(deserialized);
        Assert.Equal(255, ((Variant1)deserialized).Value);
    }

    [Fact]
    public void BcsEnum_Variant2_ShouldMatchBcsSpecification()
    {
        // Arrange - E::Variant2("e") should be [2, 1, b'e']
        var variant = new Variant2("e");

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);

        // Assert - Should match BCS specification exactly
        Assert.Equal(3, serialized.Length);
        Assert.Equal(2, serialized[0]); // Variant index 2 as ULEB128
        Assert.Equal(1, serialized[1]); // String length as ULEB128
        Assert.Equal((byte)'e', serialized[2]); // String content

        // Round-trip test
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);
        Assert.IsType<Variant2>(deserialized);
        Assert.Equal("e", ((Variant2)deserialized).Value);
    }

    [Fact]
    public void BcsEnum_UnitVariant_ShouldSerializeIndexOnly()
    {
        // Arrange - Unit variant should only serialize the index
        var variant = new UnitVariant();

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);

        // Assert - Should only contain the variant index
        Assert.Single(serialized);
        Assert.Equal(3, serialized[0]); // Variant index 3 as ULEB128

        // Round-trip test
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);
        Assert.IsType<UnitVariant>(deserialized);
    }

    [Fact]
    public void BcsEnum_ComplexVariant_ShouldSerializeAllFields()
    {
        // Arrange - Complex variant with multiple fields
        var variant = new ComplexVariant(12345, "hello", true);

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);

        // Assert - Should contain index + all field data
        Assert.True(serialized.Length > 1);
        Assert.Equal(4, serialized[0]); // Variant index 4 as ULEB128

        // Round-trip test
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);
        Assert.IsType<ComplexVariant>(deserialized);
        var complex = (ComplexVariant)deserialized;
        Assert.Equal(12345u, complex.Number);
        Assert.Equal("hello", complex.Text);
        Assert.True(complex.Flag);
    }

    [Fact]
    public void BcsEnum_StringVariant_LargeString_ShouldWork()
    {
        // Arrange - Test with larger string
        var longString = "This is a longer test string with various characters: 🦀🚀";
        var variant = new Variant2(longString);

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);

        // Assert
        Assert.IsType<Variant2>(deserialized);
        Assert.Equal(longString, ((Variant2)deserialized).Value);
        Assert.Equal(2, serialized[0]); // Variant index 2
    }

    [Fact]
    public void BcsEnum_EmptyString_ShouldWork()
    {
        // Arrange - Test with empty string
        var variant = new Variant2("");

        // Act
        var serialized = BcsSerializer.Serialize<ITestEnum>(variant);
        var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);

        // Assert
        Assert.Equal(2, serialized.Length); // Index + string length (0)
        Assert.Equal(2, serialized[0]); // Variant index 2
        Assert.Equal(0, serialized[1]); // Empty string length

        Assert.IsType<Variant2>(deserialized);
        Assert.Equal("", ((Variant2)deserialized).Value);
    }

    [Fact]
    public void BcsEnum_DeterministicSerialization_ShouldBeConsistent()
    {
        // Arrange - Same values should produce same serialization
        var variant1 = new Variant2("test");
        var variant2 = new Variant2("test");

        // Act
        var serialized1 = BcsSerializer.Serialize<ITestEnum>(variant1);
        var serialized2 = BcsSerializer.Serialize<ITestEnum>(variant2);

        // Assert - Should be identical
        Assert.Equal(serialized1, serialized2);
    }

    [Fact]
    public void BcsEnum_InvalidVariantIndex_ShouldThrowOnDeserialize()
    {
        // Arrange - Create invalid data with non-existent variant index
        var invalidData = new byte[] { 99 }; // Index 99 doesn't exist

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            BcsSerializer.Deserialize<ITestEnum>(invalidData));
    }

    [Fact]
    public void BcsEnum_ULEB128Indices_ShouldWork()
    {
        // Test that ULEB128 encoding works correctly for indices
        // For indices 0-127, ULEB128 is just the byte value
        // For larger indices, it would use multiple bytes

        var variants = new ITestEnum[]
        {
            new Variant0(100),
            new Variant1(200),
            new Variant2("test"),
            new UnitVariant(),
            new ComplexVariant(1, "x", false)
        };

        foreach (var variant in variants)
        {
            // Act
            var serialized = BcsSerializer.Serialize<ITestEnum>(variant);
            var deserialized = BcsSerializer.Deserialize<ITestEnum>(serialized);

            // Assert - Should round-trip correctly
            Assert.Equal(variant.GetType(), deserialized.GetType());

            // First byte should be a valid ULEB128 index (0-4 for our test cases)
            Assert.True(serialized[0] >= 0 && serialized[0] <= 4);
        }
    }
}
