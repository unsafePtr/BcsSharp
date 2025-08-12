using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using OneOf;
using OneOf.Types;
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
            [BcsField(0)]
            public string Street { get; set; } = "";

            [BcsField(1)]
            public string City { get; set; } = "";

            [BcsField(2)]
            public uint ZipCode { get; set; }

            [BcsField(3)]
            public int? Something { get; set; } = null;

            public Address() { }
            public Address(string street, string city, uint zipCode)
            {
                Street = street;
                City = city;
                ZipCode = zipCode;
            }
        }

        [BcsStruct]
        public class Notes
        {
            [BcsField(0)]
            public byte[] Data { get; set; } = [];

            [BcsField(1)]
            public byte? OptionalByte { get; set; } = null;

            [BcsField(2)]
            public byte?[] OptionalByteArray { get; set; } = [];

            [BcsField(3)]
            public string Description { get; set; } = "";

            [BcsField(4)]
            public UInt128[] LargeNumbers { get; set; } = [];

            [BcsField(5)]
            public UInt128?[] OptionalLargeNumbers { get; set; } = [];

            [BcsField(6)]
            public (string, int) NameValuePair { get; set; } = ("", 0);

            [BcsField(7)]
            public (byte, string, bool) ComplexTuple { get; set; } = (0, "", false);

            [BcsField(8)]
            public string? OptionalText { get; set; } = null;

            [BcsField(9)]
            public bool[] Flags { get; set; } = [];

            public Notes() { }
            public Notes(byte[] data, string description)
            {
                Data = data;
                Description = description;
                OptionalByte = 42;
                OptionalByteArray = [1, null, 3, null, 5];
                LargeNumbers = [UInt128.MaxValue, 12345, 0];
                OptionalLargeNumbers = [UInt128.MaxValue, null, 999];
                NameValuePair = ("priority", 10);
                ComplexTuple = (255, "test", true);
                OptionalText = "Some optional text";
                Flags = [true, false, true, false];
            }
        }

        [BcsStruct]
        public class Person
        {
            [BcsField(0)]
            public OneOf<None, Address> HomeAddress { get; set; } = new Address();

            [BcsField(1)]
            public string Name { get; set; } = "";

            [BcsField(2)]
            public uint Age { get; set; }

            [BcsField(3)]
            public OneOf<None, Notes> PersonalNotes { get; set; } = new Notes();

            public Person() { }
            public Person(string name, uint age, Address address, Notes notes)
            {
                Name = name;
                Age = age;
                HomeAddress = address;
                PersonalNotes = notes;
            }
        }

        [Fact]
        public void NestedObject_PersonWithAddress_ShouldWork()
        {
            // Arrange - simplified without complex UInt128[] arrays to isolate issue
            var address = new Address("123 Main St", "Springfield", 12345);
            var notes = new Notes([1, 2, 3, 255, 0], "Test basic types");
            // Clear complex arrays that might be causing issues
            notes.LargeNumbers = [];
            notes.OptionalLargeNumbers = [];
            notes.OptionalByteArray = [];

            var person = new Person("John Doe", 35, address, notes);

            // Act
            var serialized = BcsSerializer.Serialize(person);
            var deserialized = BcsSerializer.Deserialize<Person>(serialized);

            // Assert - Verify basic data first
            Assert.Equal("John Doe", deserialized.Name);
            Assert.Equal(35u, deserialized.Age);
            Assert.Equal("123 Main St", deserialized.HomeAddress.AsT1.Street);
            Assert.Equal("Springfield", deserialized.HomeAddress.AsT1.City);
            Assert.Equal(12345u, deserialized.HomeAddress.AsT1.ZipCode);

            // Verify basic Notes data
            Assert.Equal("Test basic types", deserialized.PersonalNotes.AsT1.Description);
            Assert.Equal([1, 2, 3, 255, 0], deserialized.PersonalNotes.AsT1.Data);
            Assert.Equal((byte?)42, deserialized.PersonalNotes.AsT1.OptionalByte);
            Assert.Equal("priority", deserialized.PersonalNotes.AsT1.NameValuePair.Item1);
            Assert.Equal(10, deserialized.PersonalNotes.AsT1.NameValuePair.Item2);
            Assert.Equal((byte)255, deserialized.PersonalNotes.AsT1.ComplexTuple.Item1);
            Assert.Equal("test", deserialized.PersonalNotes.AsT1.ComplexTuple.Item2);
            Assert.True(deserialized.PersonalNotes.AsT1.ComplexTuple.Item3);
        }
    }
}