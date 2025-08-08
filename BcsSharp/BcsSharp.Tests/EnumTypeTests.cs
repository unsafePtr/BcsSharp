using System;
using BcsSharp.Core;
using Xunit;

namespace BcsSharp.Tests
{
    public class EnumTypeTests
    {
        [Fact]
        public void EnumType_ShouldSerializeAndDeserializeSimpleVariants()
        {
            // Arrange
            var enumType = BcsEnum.Create("Color")
                .AddVariant("Red")
                .AddVariant("Green")
                .AddVariant("Blue")
                .Build();

            // Act & Assert - Red (index 0)
            var redVariant = enumType.CreateVariant("Red");
            var redSerialized = enumType.Serialize(redVariant);
            var redDeserialized = enumType.Parse(redSerialized);
            
            Assert.Equal("Red", redDeserialized.Name);
            Assert.False(redDeserialized.HasData);
            Assert.Equal(new byte[] { 0x00 }, redSerialized); // Index 0

            // Act & Assert - Green (index 1)
            var greenVariant = enumType.CreateVariant("Green");
            var greenSerialized = enumType.Serialize(greenVariant);
            var greenDeserialized = enumType.Parse(greenSerialized);
            
            Assert.Equal("Green", greenDeserialized.Name);
            Assert.False(greenDeserialized.HasData);
            Assert.Equal(new byte[] { 0x01 }, greenSerialized); // Index 1

            // Act & Assert - Blue (index 2)
            var blueVariant = enumType.CreateVariant("Blue");
            var blueSerialized = enumType.Serialize(blueVariant);
            var blueDeserialized = enumType.Parse(blueSerialized);
            
            Assert.Equal("Blue", blueDeserialized.Name);
            Assert.False(blueDeserialized.HasData);
            Assert.Equal(new byte[] { 0x02 }, blueSerialized); // Index 2
        }

        [Fact]
        public void EnumType_ShouldSerializeAndDeserializeVariantsWithData()
        {
            // Arrange
            var enumType = BcsEnum.Create("Shape")
                .AddVariant("Circle", Bcs.U32)
                .AddVariant("Rectangle", Bcs.U32) // Simplified - just area
                .AddVariant("Point")
                .Build();

            // Act & Assert - Circle with radius
            var circleVariant = enumType.CreateVariant("Circle", 10u);
            var circleSerialized = enumType.Serialize(circleVariant);
            var circleDeserialized = enumType.Parse(circleSerialized);
            
            Assert.Equal("Circle", circleDeserialized.Name);
            Assert.True(circleDeserialized.HasData);
            Assert.Equal(10u, circleDeserialized.GetData<uint>());
            Assert.Equal(5, circleSerialized.Length); // 1 byte index + 4 bytes u32

            // Act & Assert - Rectangle with area
            var rectVariant = enumType.CreateVariant("Rectangle", 50u);
            var rectSerialized = enumType.Serialize(rectVariant);
            var rectDeserialized = enumType.Parse(rectSerialized);
            
            Assert.Equal("Rectangle", rectDeserialized.Name);
            Assert.True(rectDeserialized.HasData);
            Assert.Equal(50u, rectDeserialized.GetData<uint>());

            // Act & Assert - Point without data
            var pointVariant = enumType.CreateVariant("Point");
            var pointSerialized = enumType.Serialize(pointVariant);
            var pointDeserialized = enumType.Parse(pointSerialized);
            
            Assert.Equal("Point", pointDeserialized.Name);
            Assert.False(pointDeserialized.HasData);
            Assert.Single(pointSerialized); // Just the index
        }

        [Fact]
        public void EnumType_ShouldThrowOnUnknownVariant()
        {
            // Arrange
            var enumType = BcsEnum.Create("Color")
                .AddVariant("Red")
                .AddVariant("Green")
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => enumType.CreateVariant("Blue"));
        }

        [Fact]
        public void EnumType_ShouldThrowWhenDataRequiredButNotProvided()
        {
            // Arrange
            var enumType = BcsEnum.Create("Shape")
                .AddVariant("Circle", Bcs.U32)
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => enumType.CreateVariant("Circle"));
        }

        [Fact]
        public void EnumType_ShouldThrowWhenDataProvidedButNotAccepted()
        {
            // Arrange
            var enumType = BcsEnum.Create("Color")
                .AddVariant("Red")
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => enumType.CreateVariant("Red", 123));
        }

        [Fact]
        public void EnumType_ShouldHandleStringVariantData()
        {
            // Arrange
            var enumType = BcsEnum.Create("Message")
                .AddVariant("Text", Bcs.String)
                .AddVariant("Empty")
                .Build();

            // Act
            var textVariant = enumType.CreateVariant("Text", "Hello World");
            var serialized = enumType.Serialize(textVariant);
            var deserialized = enumType.Parse(serialized);

            // Assert
            Assert.Equal("Text", deserialized.Name);
            Assert.True(deserialized.HasData);
            Assert.Equal("Hello World", deserialized.GetData<string>());
        }

        [Fact]
        public void EnumVariant_ShouldImplementEqualityCorrectly()
        {
            // Arrange
            var variant1 = new EnumVariant("Test", 42);
            var variant2 = new EnumVariant("Test", 42);
            var variant3 = new EnumVariant("Test", 43);
            var variant4 = new EnumVariant("Other", 42);

            // Act & Assert
            Assert.Equal(variant1, variant2);
            Assert.NotEqual(variant1, variant3);
            Assert.NotEqual(variant1, variant4);
        }

        [Fact]
        public void EnumVariant_ShouldHaveCorrectToString()
        {
            // Arrange
            var variantWithData = new EnumVariant("Circle", 10);
            var variantWithoutData = new EnumVariant("Point");

            // Act & Assert
            Assert.Equal("Circle(10)", variantWithData.ToString());
            Assert.Equal("Point", variantWithoutData.ToString());
        }

        [Fact]
        public void EnumType_ShouldCalculateSerializedSizeCorrectly()
        {
            // Arrange
            var enumType = BcsEnum.Create("Test")
                .AddVariant("Simple")
                .AddVariant("WithU32", Bcs.U32)
                .Build();

            // Act & Assert
            var simpleVariant = enumType.CreateVariant("Simple");
            Assert.Equal(1, enumType.SerializedSize(simpleVariant));

            var withDataVariant = enumType.CreateVariant("WithU32", 123u);
            Assert.Equal(5, enumType.SerializedSize(withDataVariant)); // 1 byte index + 4 bytes u32
        }

        [Fact]
        public void EnumType_ShouldThrowOnDeserializingInvalidIndex()
        {
            // Arrange
            var enumType = BcsEnum.Create("Color")
                .AddVariant("Red")
                .AddVariant("Green")
                .Build();

            var invalidData = new byte[] { 0x02 }; // Index 2 doesn't exist

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => enumType.Parse(invalidData));
        }
    }
}