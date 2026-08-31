using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

public class BcsObjectSerializationTests
{
    #region Test Object Definitions

    /// <summary>
    /// Simple struct with basic fields
    /// </summary>
    [BcsStruct]
    public class Person
    {
        [BcsField(0)]
        public string Name { get; set; } = "";

        [BcsField(1)]
        public uint Age { get; set; }

        public Person() { }
        public Person(string name, uint age)
        {
            Name = name;
            Age = age;
        }
    }

    /// <summary>
    /// Struct with explicit field ordering
    /// </summary>
    [BcsStruct]
    public class OrderedStruct
    {
        [BcsField(2)]
        public byte Third { get; set; }

        [BcsField(0)]
        public uint First { get; set; }

        [BcsField(1)]
        public string Second { get; set; } = "";

        public OrderedStruct() { }
        public OrderedStruct(uint first, string second, byte third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }

    /// <summary>
    /// Value type (struct) with BCS serialization
    /// </summary>
    [BcsStruct]
    public struct Point
    {
        [BcsField(0)]
        public int X { get; set; }

        [BcsField(1)]
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Nested object structure
    /// </summary>
    [BcsStruct]
    public class Address
    {
        [BcsField(0)]
        public string Street { get; set; } = "";

        [BcsField(1)]
        public string City { get; set; } = "";

        public Address() { }
        public Address(string street, string city)
        {
            Street = street;
            City = city;
        }
    }

    [BcsStruct]
    public class PersonWithAddress
    {
        [BcsField(0)]
        public string Name { get; set; } = "";

        [BcsField(1)]
        public Address HomeAddress { get; set; } = new Address();

        public PersonWithAddress() { }
        public PersonWithAddress(string name, Address address)
        {
            Name = name;
            HomeAddress = address;
        }
    }

    #endregion

    [Fact]
    public void BcsObject_ExplicitOrdering_ShouldRespectOrder()
    {
        // Arrange - Fields should be serialized in explicit order: First, Second, Third
        var obj = new OrderedStruct(12345, "hello", 255);

        // Act
        var serialized = BcsSerializer.Serialize(obj);
        var deserialized = BcsSerializer.Deserialize<OrderedStruct>(serialized);

        // Assert
        Assert.Equal(12345u, deserialized.First);
        Assert.Equal("hello", deserialized.Second);
        Assert.Equal(255, deserialized.Third);

        // First field should be at the beginning
        Assert.Equal(12345u, BitConverter.ToUInt32(serialized, 0));
    }

    [Fact]
    public void BcsObject_ValueType_ShouldWork()
    {
        // Arrange - Test value type (struct) serialization
        var point = new Point(100, 200);

        // Act
        var serialized = BcsSerializer.Serialize(point);
        var deserialized = BcsSerializer.Deserialize<Point>(serialized);

        // Assert
        Assert.Equal(100, deserialized.X);
        Assert.Equal(200, deserialized.Y);
        Assert.Equal(8, serialized.Length); // 2 int32 values = 8 bytes
    }

    [Fact]
    public void BcsObject_NestedObjects_ShouldWork()
    {
        // Arrange - Test nested object serialization
        var address = new Address("123 Main St", "Springfield");
        var person = new PersonWithAddress("Bob", address);

        // Act
        var serialized = BcsSerializer.Serialize(person);
        var deserialized = BcsSerializer.Deserialize<PersonWithAddress>(serialized);

        // Assert
        Assert.Equal("Bob", deserialized.Name);
        Assert.Equal("123 Main St", deserialized.HomeAddress.Street);
        Assert.Equal("Springfield", deserialized.HomeAddress.City);
    }

    [Fact]
    public void BcsObject_DeterministicSerialization_ShouldBeConsistent()
    {
        // Arrange - Same object should produce identical serialization
        var person1 = new Person("Charlie", 25);
        var person2 = new Person("Charlie", 25);

        // Act
        var serialized1 = BcsSerializer.Serialize(person1);
        var serialized2 = BcsSerializer.Serialize(person2);

        // Assert
        Assert.Equal(serialized1, serialized2);
    }

    [Fact]
    public void BcsObject_EmptyStrings_ShouldWork()
    {
        // Arrange - Test with empty strings
        var person = new Person("", 0);

        // Act
        var serialized = BcsSerializer.Serialize(person);
        var deserialized = BcsSerializer.Deserialize<Person>(serialized);

        // Assert
        Assert.Equal("", deserialized.Name);
        Assert.Equal(0u, deserialized.Age);
    }

    [Fact]
    public void BcsObject_LargeValues_ShouldWork()
    {
        // Arrange - Test with larger values
        var longName = "This is a very long name that contains many characters and should still serialize correctly! 🦀🚀";
        var person = new Person(longName, uint.MaxValue);

        // Act
        var serialized = BcsSerializer.Serialize(person);
        var deserialized = BcsSerializer.Deserialize<Person>(serialized);

        // Assert
        Assert.Equal(longName, deserialized.Name);
        Assert.Equal(uint.MaxValue, deserialized.Age);
    }
}
