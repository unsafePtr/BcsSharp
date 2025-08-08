using System;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    public class AutoStructSerializationTests
    {
        #region Test Data Classes

        public class SimplePerson
        {
            public string Name { get; set; } = string.Empty;
            public uint Age { get; set; }
            public bool IsActive { get; set; }
        }

        public class NumericData
        {
            public byte ByteValue { get; set; }
            public ushort UShortValue { get; set; }
            public uint UIntValue { get; set; }
            public ulong ULongValue { get; set; }
            public int IntValue { get; set; }
            public long LongValue { get; set; }
        }

        public class ArrayData
        {
            public string[] Tags { get; set; } = [];
            public uint[] Numbers { get; set; } = [];
            public byte[] Bytes { get; set; } = [];
        }

        // This class shows property declaration order matters
        public class OrderedProperties
        {
            public string First { get; set; } = "";
            public uint Second { get; set; }
            public bool Third { get; set; }
        }

        #endregion

        #region Auto-Discovery API Tests

        [Fact]
        public void AutoStruct_SimpleSerialization_ShouldWork()
        {
            // Arrange - Zero configuration! Properties auto-discovered in declaration order
            var personType = BcsStruct.Create<SimplePerson>();

            var testPerson = new SimplePerson
            {
                Name = "Alice",
                Age = 25,
                IsActive = true
            };

            // Act
            var serialized = personType.Serialize(testPerson);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal(testPerson.Name, deserialized.Name);
            Assert.Equal(testPerson.Age, deserialized.Age);
            Assert.Equal(testPerson.IsActive, deserialized.IsActive);
        }

        [Fact]
        public void AutoStruct_NumericTypes_ShouldWork()
        {
            // Arrange - All numeric types auto-detected
            var numericType = BcsStruct.Create<NumericData>();

            var testData = new NumericData
            {
                ByteValue = 255,
                UShortValue = 65535,
                UIntValue = 4000000000,
                ULongValue = 18000000000000000000,
                IntValue = -2000000000,
                LongValue = -9000000000000000000
            };

            // Act
            var serialized = numericType.Serialize(testData);
            var deserialized = numericType.Parse(serialized);

            // Assert
            Assert.Equal(testData.ByteValue, deserialized.ByteValue);
            Assert.Equal(testData.UShortValue, deserialized.UShortValue);
            Assert.Equal(testData.UIntValue, deserialized.UIntValue);
            Assert.Equal(testData.ULongValue, deserialized.ULongValue);
            Assert.Equal(testData.IntValue, deserialized.IntValue);
            Assert.Equal(testData.LongValue, deserialized.LongValue);
        }

        [Fact]
        public void AutoStruct_ArrayFields_ShouldWork()
        {
            // Arrange - Array properties auto-detected as vectors
            var arrayType = BcsStruct.Create<ArrayData>();

            var testData = new ArrayData
            {
                Tags = new[] { "auto", "discovery", "test" },
                Numbers = new uint[] { 1, 2, 3, 4, 5 },
                Bytes = new byte[] { 0x01, 0x02, 0x03 }
            };

            // Act
            var serialized = arrayType.Serialize(testData);
            var deserialized = arrayType.Parse(serialized);

            // Assert
            Assert.Equal(testData.Tags, deserialized.Tags);
            Assert.Equal(testData.Numbers, deserialized.Numbers);
            Assert.Equal(testData.Bytes, deserialized.Bytes);
        }

        [Fact]
        public void AutoStruct_CustomTypeName_ShouldWork()
        {
            // Arrange - Custom type name
            var personType = BcsStruct.Create<SimplePerson>("CustomPersonType");

            var testPerson = new SimplePerson
            {
                Name = "Bob",
                Age = 30,
                IsActive = false
            };

            // Act
            var serialized = personType.Serialize(testPerson);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal("CustomPersonType", personType.Name);
            Assert.Equal(testPerson.Name, deserialized.Name);
            Assert.Equal(testPerson.Age, deserialized.Age);
            Assert.Equal(testPerson.IsActive, deserialized.IsActive);
        }

        [Fact]
        public void AutoStruct_PropertyOrder_ShouldMatter()
        {
            // Arrange - Two types with same properties but different order
            var type1 = BcsStruct.Create<OrderedProperties>();

            var testData = new OrderedProperties
            {
                First = "test",
                Second = 42,
                Third = true
            };

            // Act - Serialize with the auto-discovered order
            var serialized = type1.Serialize(testData);
            var deserialized = type1.Parse(serialized);

            // Assert - Should work correctly with declaration order
            Assert.Equal(testData.First, deserialized.First);
            Assert.Equal(testData.Second, deserialized.Second);
            Assert.Equal(testData.Third, deserialized.Third);
        }

        [Fact]
        public void AutoStruct_EmptyValues_ShouldWork()
        {
            // Arrange - Test with empty/default values
            var personType = BcsStruct.Create<SimplePerson>();

            var testPerson = new SimplePerson
            {
                Name = "",
                Age = 0,
                IsActive = false
            };

            // Act
            var serialized = personType.Serialize(testPerson);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal("", deserialized.Name);
            Assert.Equal(0u, deserialized.Age);
            Assert.False(deserialized.IsActive);
        }

        [Fact]
        public void AutoStruct_DefaultTypeName_ShouldUseClassName()
        {
            // Arrange & Act
            var personType = BcsStruct.Create<SimplePerson>();

            // Assert
            Assert.Equal("SimplePerson", personType.Name);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void AutoStruct_NullValue_ShouldThrow()
        {
            // Arrange
            var personType = BcsStruct.Create<SimplePerson>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => personType.Serialize(null!));
        }

        [Fact]
        public void AutoStruct_InvalidData_ShouldThrow()
        {
            // Arrange
            var personType = BcsStruct.Create<SimplePerson>();

            // Create invalid serialized data (too short)
            var invalidData = new byte[] { 0x00 };

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => personType.Parse(invalidData));
        }

        #endregion
    }
}