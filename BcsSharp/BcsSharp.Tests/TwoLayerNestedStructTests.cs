using System;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    public class TwoLayerNestedStructTests
    {
        #region 2-Layer Test Classes

        // Layer 1: Simple nested class
        public class Address
        {
            public string Street { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public uint ZipCode { get; set; }
        }

        // Layer 2: Class containing nested custom class
        public class Person
        {
            public string Name { get; set; } = string.Empty;
            public uint Age { get; set; }
            public Address HomeAddress { get; set; } = new();
        }

        #endregion

        [Fact]
        public void TwoLayerNestedStruct_SimpleNesting_ShouldWork()
        {
            // Arrange - Simple 2-layer nested structure
            var person = new Person
            {
                Name = "John Doe",
                Age = 30,
                HomeAddress = new Address
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    ZipCode = 12345
                }
            };

            // Act
            var personType = BcsStruct.Create<Person>();
            var serialized = personType.Serialize(person);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal(person.Name, deserialized.Name);
            Assert.Equal(person.Age, deserialized.Age);
            Assert.Equal(person.HomeAddress.Street, deserialized.HomeAddress.Street);
            Assert.Equal(person.HomeAddress.City, deserialized.HomeAddress.City);
            Assert.Equal(person.HomeAddress.ZipCode, deserialized.HomeAddress.ZipCode);
        }

        [Fact]
        public void TwoLayerNestedStruct_NestedClassCaching_ShouldWork()
        {
            // Arrange - Test that nested classes are properly cached
            var person1 = new Person
            {
                Name = "Alice",
                Age = 25,
                HomeAddress = new Address { Street = "456 Oak St", City = "Springfield", ZipCode = 54321 }
            };

            var person2 = new Person
            {
                Name = "Bob", 
                Age = 35,
                HomeAddress = new Address { Street = "789 Pine Ave", City = "Riverside", ZipCode = 67890 }
            };

            // Act - Multiple serializations should reuse cached nested types
            var personType1 = BcsStruct.Create<Person>();
            var personType2 = BcsStruct.Create<Person>();
            var addressType = BcsStruct.Create<Address>(); // Should reuse the cached Address from Person

            var serialized1 = personType1.Serialize(person1);
            var serialized2 = personType2.Serialize(person2);
            var addressSerialized = addressType.Serialize(person1.HomeAddress);

            var deserialized1 = personType1.Parse(serialized1);
            var deserialized2 = personType2.Parse(serialized2);
            var addressDeserialized = addressType.Parse(addressSerialized);

            // Assert
            Assert.Equal(person1.Name, deserialized1.Name);
            Assert.Equal(person1.HomeAddress.Street, deserialized1.HomeAddress.Street);
            Assert.Equal(person2.Name, deserialized2.Name);
            Assert.Equal(person2.HomeAddress.City, deserialized2.HomeAddress.City);
            Assert.Equal(person1.HomeAddress.Street, addressDeserialized.Street);
        }
    }
}