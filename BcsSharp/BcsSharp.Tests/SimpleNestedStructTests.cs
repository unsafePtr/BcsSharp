using System;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Nethermind.Int256;
using Xunit;

namespace BcsSharp.Tests
{
    public class SimpleNestedStructTests
    {
        #region Test Data Classes - Only Basic Types (What Currently Works)

        public class BasicPerson
        {
            public string Name { get; set; } = string.Empty;
            public uint Age { get; set; }
            public bool IsActive { get; set; }
            public string[] Skills { get; set; } = [];
            public uint[] Scores { get; set; } = [];
        }

        public class NumericContainer
        {
            public byte ByteValue { get; set; }
            public ushort UShortValue { get; set; }
            public uint UIntValue { get; set; }
            public ulong ULongValue { get; set; }
            public UInt128 UInt128Value { get; set; }
            public UInt256 UInt256Value { get; set; }
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public Int128 Int128Value { get; set; }
        }

        public class ArrayContainer
        {
            public byte[] Bytes { get; set; } = [];
            public ushort[] UShorts { get; set; } = [];
            public uint[] UInts { get; set; } = [];
            public ulong[] ULongs { get; set; } = [];
            public UInt128[] UInt128s { get; set; } = [];
            public UInt256[] UInt256s { get; set; } = [];
            public int[] Ints { get; set; } = [];
            public long[] Longs { get; set; } = [];
            public Int128[] Int128s { get; set; } = [];
            public bool[] Bools { get; set; } = [];
            public string[] Strings { get; set; } = [];
        }

        #endregion

        [Fact]
        public void SimpleNestedStruct_BasicPersonWithArrays_ShouldWork()
        {
            // Arrange - Test the current auto-discovery capabilities
            var person = new BasicPerson
            {
                Name = "John Doe",
                Age = 30,
                IsActive = true,
                Skills = new[] { "C#", "TypeScript", "Rust" },
                Scores = new uint[] { 95, 87, 92, 88 }
            };

            // Act - Use auto-discovery
            var personType = BcsStruct.Create<BasicPerson>();
            var serialized = personType.Serialize(person);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal(person.Name, deserialized.Name);
            Assert.Equal(person.Age, deserialized.Age);
            Assert.Equal(person.IsActive, deserialized.IsActive);
            Assert.Equal(person.Skills, deserialized.Skills);
            Assert.Equal(person.Scores, deserialized.Scores);
        }

        [Fact]
        public void SimpleNestedStruct_AllNumericTypes_ShouldWork()
        {
            // Arrange - Test all numeric types supported by our cache
            var container = new NumericContainer
            {
                ByteValue = 255,
                UShortValue = 65535,
                UIntValue = 4000000000,
                ULongValue = 18000000000000000000,
                UInt128Value = new UInt128(0x123456789ABCDEF0, 0xFEDCBA9876543210),
                UInt256Value = UInt256.MaxValue,
                IntValue = -2000000000,
                LongValue = -9000000000000000000,
                Int128Value = new Int128(0x0000000000000001, 0x8000000000000000)
            };

            // Act
            var containerType = BcsStruct.Create<NumericContainer>();
            var serialized = containerType.Serialize(container);
            var deserialized = containerType.Parse(serialized);

            // Assert
            Assert.Equal(container.ByteValue, deserialized.ByteValue);
            Assert.Equal(container.UShortValue, deserialized.UShortValue);
            Assert.Equal(container.UIntValue, deserialized.UIntValue);
            Assert.Equal(container.ULongValue, deserialized.ULongValue);
            Assert.Equal(container.UInt128Value, deserialized.UInt128Value);
            Assert.Equal(container.UInt256Value, deserialized.UInt256Value);
            Assert.Equal(container.IntValue, deserialized.IntValue);
            Assert.Equal(container.LongValue, deserialized.LongValue);
            Assert.Equal(container.Int128Value, deserialized.Int128Value);
        }

        [Fact]
        public void SimpleNestedStruct_AllArrayTypes_ShouldWork()
        {
            // Arrange - Test all array types in our cache
            var container = new ArrayContainer
            {
                Bytes = new byte[] { 1, 2, 3, 255 },
                UShorts = new ushort[] { 1000, 2000, 65535 },
                UInts = new uint[] { 1000000, 2000000, 4000000000 },
                ULongs = new ulong[] { 1000000000000, 18000000000000000000 },
                UInt128s = new UInt128[] { new UInt128(100, 200), UInt128.MaxValue },
                UInt256s = new UInt256[] { new UInt256(999888777666555UL), UInt256.MaxValue },
                Ints = new int[] { -1000000, 1000000, -2000000000 },
                Longs = new long[] { -1000000000000, 1000000000000, -9000000000000000000 },
                Int128s = new Int128[] { Int128.MinValue, Int128.MaxValue },
                Bools = new bool[] { true, false, true, true, false },
                Strings = new string[] { "Hello", "World", "BCS", "Serialization" }
            };

            // Act
            var containerType = BcsStruct.Create<ArrayContainer>();
            var serialized = containerType.Serialize(container);
            var deserialized = containerType.Parse(serialized);

            // Assert
            Assert.Equal(container.Bytes, deserialized.Bytes);
            Assert.Equal(container.UShorts, deserialized.UShorts);
            Assert.Equal(container.UInts, deserialized.UInts);
            Assert.Equal(container.ULongs, deserialized.ULongs);
            Assert.Equal(container.UInt128s, deserialized.UInt128s);
            Assert.Equal(container.UInt256s, deserialized.UInt256s);
            Assert.Equal(container.Ints, deserialized.Ints);
            Assert.Equal(container.Longs, deserialized.Longs);
            Assert.Equal(container.Int128s, deserialized.Int128s);
            Assert.Equal(container.Bools, deserialized.Bools);
            Assert.Equal(container.Strings, deserialized.Strings);
        }

        [Fact]
        public void SimpleNestedStruct_CachePerformance_ShouldBeEfficient()
        {
            // Arrange - Create multiple instances to test caching
            var person1 = new BasicPerson { Name = "Alice", Age = 25, IsActive = true, Skills = new[] { "C#" }, Scores = new uint[] { 95 } };
            var person2 = new BasicPerson { Name = "Bob", Age = 30, IsActive = false, Skills = new[] { "Rust" }, Scores = new uint[] { 88 } };
            var nums = new NumericContainer { ByteValue = 100, UIntValue = 5000, Int128Value = new Int128(0, 42) };

            // Act - Multiple struct type creations should reuse cached factories
            var startTime = DateTime.UtcNow;
            
            var personType1 = BcsStruct.Create<BasicPerson>();
            var personType2 = BcsStruct.Create<BasicPerson>();
            var numType = BcsStruct.Create<NumericContainer>();
            
            var serialized1 = personType1.Serialize(person1);
            var serialized2 = personType2.Serialize(person2);
            var numSerialized = numType.Serialize(nums);
            
            var deserialized1 = personType1.Parse(serialized1);
            var deserialized2 = personType2.Parse(serialized2);
            var numDeserialized = numType.Parse(numSerialized);
            
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.Equal(person1.Name, deserialized1.Name);
            Assert.Equal(person2.Name, deserialized2.Name);
            Assert.Equal(nums.ByteValue, numDeserialized.ByteValue);
            Assert.True(elapsed.TotalMilliseconds < 100, $"Cache performance test took {elapsed.TotalMilliseconds}ms, expected < 100ms");
        }

        [Fact]
        public void SimpleNestedStruct_EmptyArrays_ShouldWork()
        {
            // Arrange - Test with empty arrays
            var person = new BasicPerson
            {
                Name = "",
                Age = 0,
                IsActive = false,
                Skills = [], // Empty array
                Scores = []  // Empty array
            };

            // Act
            var personType = BcsStruct.Create<BasicPerson>();
            var serialized = personType.Serialize(person);
            var deserialized = personType.Parse(serialized);

            // Assert
            Assert.Equal("", deserialized.Name);
            Assert.Equal(0u, deserialized.Age);
            Assert.False(deserialized.IsActive);
            Assert.Empty(deserialized.Skills);
            Assert.Empty(deserialized.Scores);
        }

        [Fact]
        public void SimpleNestedStruct_LargeArrays_ShouldWork()
        {
            // Arrange - Test with larger arrays to verify performance
            var largeSkills = new string[1000];
            var largeScores = new uint[1000];
            
            for (int i = 0; i < 1000; i++)
            {
                largeSkills[i] = $"Skill{i}";
                largeScores[i] = (uint)(i * 10);
            }

            var person = new BasicPerson
            {
                Name = "Large Data Person",
                Age = 35,
                IsActive = true,
                Skills = largeSkills,
                Scores = largeScores
            };

            // Act
            var startTime = DateTime.UtcNow;
            var personType = BcsStruct.Create<BasicPerson>();
            var serialized = personType.Serialize(person);
            var deserialized = personType.Parse(serialized);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.Equal(person.Name, deserialized.Name);
            Assert.Equal(1000, deserialized.Skills.Length);
            Assert.Equal(1000, deserialized.Scores.Length);
            Assert.Equal("Skill999", deserialized.Skills[999]);
            Assert.Equal(9990u, deserialized.Scores[999]);
            Assert.True(elapsed.TotalMilliseconds < 500, $"Large array test took {elapsed.TotalMilliseconds}ms, expected < 500ms");
        }
    }
}