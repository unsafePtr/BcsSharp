using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Working example of nested object serialization
    /// </summary>
    public class WorkingNestedObjectExample
    {
        [BcsStruct]
        public class Address
        {
            [BcsField]
            public string Street { get; set; } = "";

            [BcsField]
            public string City { get; set; } = "";

            [BcsField]
            public uint ZipCode { get; set; }

            public Address() { }
            public Address(string street, string city, uint zipCode)
            {
                Street = street;
                City = city;
                ZipCode = zipCode;
            }
        }

        [BcsStruct]
        public class Person
        {
            [BcsField]
            public Address HomeAddress { get; set; } = new Address();

            [BcsField]
            public string Name { get; set; } = "";

            [BcsField]
            public uint Age { get; set; }

            public Person() { }
            public Person(string name, uint age, Address address)
            {
                Name = name;
                Age = age;
                HomeAddress = address;
            }
        }

        [Fact]
        public void NestedObject_PersonWithAddress_ShouldWork()
        {
            // Arrange
            var address = new Address("123 Main St", "Springfield", 12345);
            var person = new Person("John Doe", 35, address);

            // Act
            var serialized = BcsSerializer.Serialize(person);
            var deserialized = BcsSerializer.Deserialize<Person>(serialized);

            // Assert - Verify all nested data
            Assert.Equal("John Doe", deserialized.Name);
            Assert.Equal(35u, deserialized.Age);
            Assert.Equal("123 Main St", deserialized.HomeAddress.Street);
            Assert.Equal("Springfield", deserialized.HomeAddress.City);
            Assert.Equal(12345u, deserialized.HomeAddress.ZipCode);
        }
    }
}