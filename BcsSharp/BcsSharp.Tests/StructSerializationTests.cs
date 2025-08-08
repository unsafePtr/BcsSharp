using System;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    public class StructSerializationTests
    {
        #region Test Data Classes

        public class Person
        {
            public string Name { get; set; } = string.Empty;
            public uint Age { get; set; }
            public bool IsActive { get; set; }
        }

        public class ComplexData
        {
            public long Id { get; set; }
            public string[] Tags { get; set; } = Array.Empty<string>();
            public byte Status { get; set; }
        }

        #endregion

        #region Fully Generic Implementation Tests

        [Fact]
        public void StructType_Manual_BasicSerialization_ShouldWork()
        {
            // Arrange - Using manual builder for advanced scenarios
            var personType = BcsStruct.CreateManual<Person>("Person")
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .AddField("Age", Bcs.U32, p => p.Age, (p, v) => p.Age = v)
                .AddField("IsActive", Bcs.Bool, p => p.IsActive, (p, v) => p.IsActive = v)
                .Build();

            var testPerson = new Person
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
        public void StructType_FullyGeneric_WithArrayField_ShouldWork()
        {
            // Arrange
            var complexType = BcsStruct.CreateManual<ComplexData>("ComplexData")
                .AddField("Id", Bcs.I64, c => c.Id, (c, v) => c.Id = v)
                .AddField("Tags", Bcs.Vector(Bcs.String), c => c.Tags, (c, v) => c.Tags = v)
                .AddField("Status", Bcs.U8, c => c.Status, (c, v) => c.Status = v)
                .Build();

            var testData = new ComplexData
            {
                Id = -12345,
                Tags = new[] { "important", "urgent", "review" },
                Status = 42
            };

            // Act
            var serialized = complexType.Serialize(testData);
            var deserialized = complexType.Parse(serialized);

            // Assert
            Assert.Equal(testData.Id, deserialized.Id);
            Assert.Equal(testData.Tags, deserialized.Tags);
            Assert.Equal(testData.Status, deserialized.Status);
        }

        [Fact]
        public void StructType_FullyGeneric_SerializedSize_WithFixedFields_ShouldWork()
        {
            // Arrange - Use only fixed-size fields
            var dataType = BcsStruct.CreateManual<ComplexData>("ComplexData")
                .AddField("Id", Bcs.I64, c => c.Id, (c, v) => c.Id = v)
                .AddField("Status", Bcs.U8, c => c.Status, (c, v) => c.Status = v)
                .Build();

            var testData = new ComplexData
            {
                Id = -12345, // 8 bytes
                Status = 42  // 1 byte
            };

            // Act
            var actualSerialized = dataType.Serialize(testData);
            var calculatedSize = dataType.SerializedSize(testData);

            // Assert
            Assert.NotNull(calculatedSize);
            Assert.Equal(actualSerialized.Length, calculatedSize.Value);
            Assert.Equal(9, actualSerialized.Length); // 8 + 1 = 9 bytes
        }

        [Fact]
        public void StructType_FullyGeneric_EmptyStringField_ShouldWork()
        {
            // Arrange
            var personType = BcsStruct.CreateManual<Person>("Person")
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .AddField("Age", Bcs.U32, p => p.Age, (p, v) => p.Age = v)
                .Build();

            var testPerson = new Person
            {
                Name = "",
                Age = 0
            };

            // Act
            var serialized = personType.Serialize(testPerson);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal("", deserialized.Name);
            Assert.Equal(0u, deserialized.Age);
        }

        [Fact]
        public void StructType_FullyGeneric_FieldOrder_ShouldBePreserved()
        {
            // Arrange - Create two struct types with different field order
            var type1 = BcsStruct.CreateManual<Person>("Person1")
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .AddField("Age", Bcs.U32, p => p.Age, (p, v) => p.Age = v)
                .Build();

            var type2 = BcsStruct.CreateManual<Person>("Person2")
                .AddField("Age", Bcs.U32, p => p.Age, (p, v) => p.Age = v)
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .Build();

            var testPerson = new Person
            {
                Name = "Test",
                Age = 25
            };

            // Act
            var serialized1 = type1.Serialize(testPerson);
            var serialized2 = type2.Serialize(testPerson);

            // Assert - Different field orders should produce different serializations
            Assert.NotEqual(serialized1, serialized2);
            
            // But each should deserialize correctly with its own type
            var deserialized1 = type1.Parse(serialized1);
            var deserialized2 = type2.Parse(serialized2);

            Assert.Equal(testPerson.Name, deserialized1.Name);
            Assert.Equal(testPerson.Age, deserialized1.Age);
            Assert.Equal(testPerson.Name, deserialized2.Name);
            Assert.Equal(testPerson.Age, deserialized2.Age);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void StructType_FullyGeneric_NullValue_ShouldThrow()
        {
            // Arrange
            var personType = BcsStruct.CreateManual<Person>("Person")
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => personType.Serialize(null!));
        }

        [Fact]
        public void StructType_FullyGeneric_InvalidData_ShouldThrow()
        {
            // Arrange
            var personType = BcsStruct.CreateManual<Person>("Person")
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .AddField("Age", Bcs.U32, p => p.Age, (p, v) => p.Age = v)
                .Build();

            // Create invalid serialized data (too short)
            var invalidData = new byte[] { 0x00 };

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => personType.Parse(invalidData));
        }

        #endregion

        #region Auto vs Manual API Comparison

        [Fact]
        public void BcsStruct_AutoVsManual_ShouldProduceSameResult()
        {
            // Arrange - Same class, auto vs manual configuration
            var autoType = BcsStruct.Create<Person>(); // Auto-discovery (default)
            var manualType = BcsStruct.CreateManual<Person>("Person") // Manual builder
                .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
                .AddField("Age", Bcs.U32, p => p.Age, (p, v) => p.Age = v)
                .AddField("IsActive", Bcs.Bool, p => p.IsActive, (p, v) => p.IsActive = v)
                .Build();

            var testPerson = new Person
            {
                Name = "Comparison",
                Age = 35,
                IsActive = true
            };

            // Act
            var autoSerialized = autoType.Serialize(testPerson);
            var manualSerialized = manualType.Serialize(testPerson);

            // Assert - Should produce identical serialization
            Assert.Equal(autoSerialized, manualSerialized);

            // Both should deserialize correctly
            var autoDeserialized = autoType.Parse(autoSerialized);
            var manualDeserialized = manualType.Parse(manualSerialized);

            Assert.Equal(testPerson.Name, autoDeserialized.Name);
            Assert.Equal(testPerson.Age, autoDeserialized.Age);
            Assert.Equal(testPerson.IsActive, autoDeserialized.IsActive);
            
            Assert.Equal(testPerson.Name, manualDeserialized.Name);
            Assert.Equal(testPerson.Age, manualDeserialized.Age);
            Assert.Equal(testPerson.IsActive, manualDeserialized.IsActive);
        }

        [Fact]
        public void BcsStruct_DefaultCreate_ShouldBeAutoDiscovery()
        {
            // Arrange & Act - Default Create should use auto-discovery
            var personType = BcsStruct.Create<Person>(); // Should be auto-discovery

            // Assert - Should auto-detect properties and work
            var testPerson = new Person { Name = "Auto", Age = 25, IsActive = true };
            var serialized = personType.Serialize(testPerson);
            var deserialized = personType.Parse(serialized);

            Assert.Equal(testPerson.Name, deserialized.Name);
            Assert.Equal(testPerson.Age, deserialized.Age);
            Assert.Equal(testPerson.IsActive, deserialized.IsActive);
        }

        #endregion

        #region Expression-Based API Tests

        [Fact]
        public void StructType_ExpressionAPI_BasicSerialization_ShouldWork()
        {
            // Arrange - Using the new expression-based API (much cleaner!)
            var personType = BcsStruct.CreateManual<Person>("Person")
                .AddField(Bcs.String, p => p.Name)     // ✅ Auto-generated getter/setter!
                .AddField(Bcs.U32, p => p.Age)         // ✅ Property name inferred!
                .AddField(Bcs.Bool, p => p.IsActive)   // ✅ No boilerplate!
                .Build();

            var testPerson = new Person
            {
                Name = "Bob",
                Age = 30,
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
        public void StructType_ExpressionAPI_WithCustomNames_ShouldWork()
        {
            // Arrange - Custom field names with expression API
            var personType = BcsStruct.CreateManual<Person>("Person")
                .AddField("full_name", Bcs.String, p => p.Name)      // Custom name
                .AddField("years_old", Bcs.U32, p => p.Age)          // Custom name
                .AddField("active_status", Bcs.Bool, p => p.IsActive) // Custom name
                .Build();

            var testPerson = new Person
            {
                Name = "Charlie",
                Age = 35,
                IsActive = false
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
        public void StructType_ExpressionAPI_ComplexFields_ShouldWork()
        {
            // Arrange - Complex fields with expression API
            var complexType = BcsStruct.CreateManual<ComplexData>("ComplexData")
                .AddField(Bcs.I64, c => c.Id)                        // ✅ Auto-inferred
                .AddField(Bcs.Vector(Bcs.String), c => c.Tags)       // ✅ Vector type
                .AddField(Bcs.U8, c => c.Status)                     // ✅ Simple
                .Build();

            var testData = new ComplexData
            {
                Id = -999999,
                Tags = new[] { "automated", "test", "expression" },
                Status = 100
            };

            // Act
            var serialized = complexType.Serialize(testData);
            var deserialized = complexType.Parse(serialized);

            // Assert
            Assert.Equal(testData.Id, deserialized.Id);
            Assert.Equal(testData.Tags, deserialized.Tags);
            Assert.Equal(testData.Status, deserialized.Status);
        }

        [Fact]
        public void StructType_ExpressionAPI_InvalidExpression_ShouldThrow()
        {
            // Arrange & Act & Assert - Non-property expressions should throw
            var builder = BcsStruct.CreateManual<Person>("Person");
            
            // This should throw because it's not a property access
            Assert.Throws<ArgumentException>(() => 
                builder.AddField(Bcs.String, p => p.Name.ToUpper())
            );
        }

        #endregion
    }
}