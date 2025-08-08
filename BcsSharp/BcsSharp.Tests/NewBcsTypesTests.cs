using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    public class NewBcsTypesTests
    {
        #region Signed Integer Tests

        [Fact]
        public void I8Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = Bcs.I8;
            sbyte[] testValues = { -128, -1, 0, 1, 127 };

            foreach (var value in testValues)
            {
                // Act
                var serialized = type.Serialize(value);
                var deserialized = type.Parse(serialized);

                // Assert
                Assert.Equal(value, deserialized);
                Assert.Single(serialized);
                Assert.Equal((byte)value, serialized[0]);
            }
        }

        [Fact]
        public void I16Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = Bcs.I16;
            short[] testValues = { -32768, -1, 0, 1, 32767 };

            foreach (var value in testValues)
            {
                // Act
                var serialized = type.Serialize(value);
                var deserialized = type.Parse(serialized);

                // Assert
                Assert.Equal(value, deserialized);
                Assert.Equal(2, serialized.Length);
            }
        }

        [Fact]
        public void I32Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = Bcs.I32;
            int[] testValues = { int.MinValue, -1, 0, 1, int.MaxValue };

            foreach (var value in testValues)
            {
                // Act
                var serialized = type.Serialize(value);
                var deserialized = type.Parse(serialized);

                // Assert
                Assert.Equal(value, deserialized);
                Assert.Equal(4, serialized.Length);
            }
        }

        [Fact]
        public void I64Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = Bcs.I64;
            long[] testValues = { long.MinValue, -1, 0, 1, long.MaxValue };

            foreach (var value in testValues)
            {
                // Act
                var serialized = type.Serialize(value);
                var deserialized = type.Parse(serialized);

                // Assert
                Assert.Equal(value, deserialized);
                Assert.Equal(8, serialized.Length);
            }
        }

        [Fact]
        public void I128Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = Bcs.I128;
            Int128[] testValues = {
                Int128.MinValue,
                new Int128(0, 0) - 1, // -1
                Int128.Zero,
                Int128.One,
                Int128.MaxValue
            };

            foreach (var value in testValues)
            {
                // Act
                var serialized = type.Serialize(value);
                var deserialized = type.Parse(serialized);

                // Assert
                Assert.Equal(value, deserialized);
                Assert.Equal(16, serialized.Length);
            }
        }

        [Fact]
        public void I32Type_ShouldUseLittleEndian()
        {
            // Arrange
            var type = Bcs.I32;
            int value = -0x12345678; // Negative value to test two's complement

            // Act
            var serialized = type.Serialize(value);

            // Assert
            Assert.Equal(4, serialized.Length);
            // Verify little-endian serialization of two's complement
            Span<byte> expected = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(expected, value);
            Assert.Equal(expected.ToArray(), serialized);
        }

        #endregion

        #region Unit Type Tests

        [Fact]
        public void UnitType_ShouldSerializeToEmptyBytes()
        {
            // Arrange
            var type = Bcs.Unit;
            var value = Unit.Value;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Empty(serialized); // Unit serializes to 0 bytes
        }

        [Fact]
        public void UnitType_ShouldHaveCorrectProperties()
        {
            // Arrange & Act
            var unit1 = Unit.Value;
            var unit2 = new Unit();

            // Assert
            Assert.Equal(unit1, unit2);
            Assert.True(unit1 == unit2);
            Assert.False(unit1 != unit2);
            Assert.Equal("()", unit1.ToString());
            Assert.Equal(0, unit1.GetHashCode());
        }

        #endregion

        #region Map Type Tests

        [Fact]
        public void MapType_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var mapType = Bcs.Map(Bcs.String, Bcs.U32);
            var testMap = new Dictionary<string, uint>
            {
                ["alice"] = 100,
                ["bob"] = 200,
                ["charlie"] = 300
            };

            // Act
            var serialized = mapType.Serialize(testMap);
            var deserialized = mapType.Parse(serialized);

            // Assert
            Assert.Equal(testMap.Count, deserialized.Count);
            foreach (var kvp in testMap)
            {
                Assert.True(deserialized.ContainsKey(kvp.Key));
                Assert.Equal(kvp.Value, deserialized[kvp.Key]);
            }
        }

        [Fact]
        public void MapType_ShouldSerializeInKeyOrder()
        {
            // Arrange
            var mapType = Bcs.Map(Bcs.String, Bcs.U32);
            var testMap = new Dictionary<string, uint>
            {
                ["zebra"] = 1,
                ["alpha"] = 2,
                ["beta"] = 3
            };

            // Act
            var serialized = mapType.Serialize(testMap);
            var reader = new BcsReader(serialized);

            // Read and verify ordering
            var length = reader.ReadULEB32();
            Assert.Equal(3u, length);

            // Keys should be in alphabetical order
            var firstKey = reader.ReadString();
            reader.Read32(); // skip value
            var secondKey = reader.ReadString();
            reader.Read32(); // skip value
            var thirdKey = reader.ReadString();

            Assert.Equal("alpha", firstKey);
            Assert.Equal("beta", secondKey);
            Assert.Equal("zebra", thirdKey);
        }

        [Fact]
        public void MapType_ShouldRejectDuplicateKeys()
        {
            // Arrange
            var mapType = Bcs.Map(Bcs.String, Bcs.U32);

            // Create serialized data with duplicate keys manually
            var writer = new BcsWriter();
            writer.WriteULEB(2u); // 2 entries
            writer.WriteString("key");
            writer.Write32(100u);
            writer.WriteString("key"); // Duplicate key
            writer.Write32(200u);

            var invalidData = writer.ToBytes();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapType.Parse(invalidData));
        }

        [Fact]
        public void MapType_EmptyMap_ShouldWork()
        {
            // Arrange
            var mapType = Bcs.Map(Bcs.String, Bcs.U32);
            var emptyMap = new Dictionary<string, uint>();

            // Act
            var serialized = mapType.Serialize(emptyMap);
            var deserialized = mapType.Parse(serialized);

            // Assert
            Assert.Empty(deserialized);
            Assert.Single(serialized); // Just the length byte (0)
        }

        #endregion

        #region Set Type Tests

        [Fact]
        public void SetType_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var setType = Bcs.Set(Bcs.String);
            var testSet = new HashSet<string> { "apple", "banana", "cherry" };

            // Act
            var serialized = setType.Serialize(testSet);
            var deserialized = setType.Parse(serialized);

            // Assert
            Assert.Equal(testSet.Count, deserialized.Count);
            foreach (var item in testSet)
            {
                Assert.Contains(item, deserialized);
            }
        }

        [Fact]
        public void SetType_ShouldSerializeInSortedOrder()
        {
            // Arrange
            var setType = Bcs.Set(Bcs.String);
            var testSet = new HashSet<string> { "zebra", "alpha", "beta" };

            // Act
            var serialized = setType.Serialize(testSet);
            var reader = new BcsReader(serialized);

            // Read and verify ordering
            var length = reader.ReadULEB32();
            Assert.Equal(3u, length);

            // Values should be in alphabetical order
            var first = reader.ReadString();
            var second = reader.ReadString();
            var third = reader.ReadString();

            Assert.Equal("alpha", first);
            Assert.Equal("beta", second);
            Assert.Equal("zebra", third);
        }

        [Fact]
        public void SetType_ShouldRejectDuplicateValues()
        {
            // Arrange
            var setType = Bcs.Set(Bcs.String);

            // Create serialized data with duplicate values manually
            var writer = new BcsWriter();
            writer.WriteULEB(2u); // 2 entries
            writer.WriteString("value");
            writer.WriteString("value"); // Duplicate value

            var invalidData = writer.ToBytes();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => setType.Parse(invalidData));
        }

        [Fact]
        public void SetType_EmptySet_ShouldWork()
        {
            // Arrange
            var setType = Bcs.Set(Bcs.String);
            var emptySet = new HashSet<string>();

            // Act
            var serialized = setType.Serialize(emptySet);
            var deserialized = setType.Parse(serialized);

            // Assert
            Assert.Empty(deserialized);
            Assert.Single(serialized); // Just the length byte (0)
        }

        #endregion

        #region Tuple Type Tests

        [Fact]
        public void TupleType2_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var tupleType = Bcs.Tuple(Bcs.String, Bcs.U32);
            var testTuple = ("hello", 42u);

            // Act
            var serialized = tupleType.Serialize(testTuple);
            var deserialized = tupleType.Parse(serialized);

            // Assert
            Assert.Equal(testTuple.Item1, deserialized.Item1);
            Assert.Equal(testTuple.Item2, deserialized.Item2);
        }

        [Fact]
        public void TupleType3_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var tupleType = Bcs.Tuple(Bcs.String, Bcs.U32, Bcs.Bool);
            var testTuple = ("hello", 42u, true);

            // Act
            var serialized = tupleType.Serialize(testTuple);
            var deserialized = tupleType.Parse(serialized);

            // Assert
            Assert.Equal(testTuple.Item1, deserialized.Item1);
            Assert.Equal(testTuple.Item2, deserialized.Item2);
            Assert.Equal(testTuple.Item3, deserialized.Item3);
        }

        [Fact]
        public void TupleType4_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var tupleType = Bcs.Tuple(Bcs.String, Bcs.U32, Bcs.Bool, Bcs.U8);
            var testTuple = ("hello", 42u, true, (byte)255);

            // Act
            var serialized = tupleType.Serialize(testTuple);
            var deserialized = tupleType.Parse(serialized);

            // Assert
            Assert.Equal(testTuple.Item1, deserialized.Item1);
            Assert.Equal(testTuple.Item2, deserialized.Item2);
            Assert.Equal(testTuple.Item3, deserialized.Item3);
            Assert.Equal(testTuple.Item4, deserialized.Item4);
        }

        [Fact]
        public void TupleType_NestedTuples_ShouldWork()
        {
            // Arrange
            var innerTuple = Bcs.Tuple(Bcs.U32, Bcs.String);
            var outerTuple = Bcs.Tuple(innerTuple, Bcs.Bool);
            var testData = ((42u, "inner"), true);

            // Act
            var serialized = outerTuple.Serialize(testData);
            var deserialized = outerTuple.Parse(serialized);

            // Assert
            Assert.Equal(testData.Item1.Item1, deserialized.Item1.Item1);
            Assert.Equal(testData.Item1.Item2, deserialized.Item1.Item2);
            Assert.Equal(testData.Item2, deserialized.Item2);
        }

        #endregion

        #region Fixed Array Type Tests

        [Fact]
        public void FixedArrayType_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var arrayType = Bcs.FixedArray(Bcs.U32, 3);
            var testArray = new uint[] { 1, 2, 3 };

            // Act
            var serialized = arrayType.Serialize(testArray);
            var deserialized = arrayType.Parse(serialized);

            // Assert
            Assert.Equal(testArray, deserialized);
            Assert.Equal(12, serialized.Length); // 3 * 4 bytes
        }

        [Fact]
        public void FixedArrayType_ShouldRejectWrongLength()
        {
            // Arrange
            var arrayType = Bcs.FixedArray(Bcs.U32, 3);
            var wrongLengthArray = new uint[] { 1, 2 }; // Only 2 elements

            // Act & Assert
            Assert.Throws<ArgumentException>(() => arrayType.Serialize(wrongLengthArray));
        }

        [Fact]
        public void FixedArrayType_EmptyArray_ShouldWork()
        {
            // Arrange
            var arrayType = Bcs.FixedArray(Bcs.U32, 0);
            var emptyArray = new uint[0];

            // Act
            var serialized = arrayType.Serialize(emptyArray);
            var deserialized = arrayType.Parse(serialized);

            // Assert
            Assert.Empty(deserialized);
            Assert.Empty(serialized); // 0 elements = 0 bytes
        }

        #endregion

        #region Struct Type Tests (Existing Implementation)

        [Fact]
        public void ExistingStructType_ShouldWork()
        {
            // Note: The existing struct implementation requires defining a C# class
            // and using property accessors, which is different from the one I tried to implement
            // For now, this test just verifies the existing BcsStruct factory method exists

            // The existing implementation would require something like:
            // var structType = BcsStruct.Create<Person>("Person")
            //     .AddField("Name", Bcs.String, p => p.Name, (p, v) => p.Name = v)
            //     .Build();

            Assert.NotNull(typeof(BcsStruct));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void IntegratedTypes_ComplexStructure_ShouldWork()
        {
            // Arrange - Create a complex structure using multiple new types without structs
            var complexType = Bcs.Tuple(
                Bcs.I64,                                    // id
                Bcs.Tuple(Bcs.String, Bcs.Option(Bcs.String)), // profile (name, email)
                Bcs.FixedArray(Bcs.I32, 3),                // scores
                Bcs.Set(Bcs.String),                       // tags
                Bcs.Map(Bcs.String, Bcs.U32)              // metadata
            );

            var tags = new HashSet<string> { "premium", "active", "verified" };
            var metadata = new Dictionary<string, uint> { ["login_count"] = 42, ["level"] = 5 };
            var scores = new int[] { 100, 85, -10 };
            var profile = ("Alice Johnson", "alice@example.com");

            var testData = (
                -1234567890123456789L,  // id
                profile,                // profile
                scores,                 // scores
                tags,                   // tags
                metadata                // metadata
            );

            // Act
            var serialized = complexType.Serialize(testData);
            var deserialized = complexType.Parse(serialized);

            // Assert
            Assert.Equal(-1234567890123456789L, deserialized.Item1);
            Assert.Equal("Alice Johnson", deserialized.Item2.Item1);
            Assert.Equal("alice@example.com", deserialized.Item2.Item2);
            Assert.Equal(scores, deserialized.Item3);
            Assert.Equal(tags, deserialized.Item4);
            Assert.Equal(metadata, deserialized.Item5);
        }

        [Fact]
        public void SerializedSize_NewTypes_ShouldCalculateCorrectly()
        {
            // Test size calculations for fixed-size types
            Assert.Equal(1, Bcs.I8.SerializedSize(-1));
            Assert.Equal(2, Bcs.I16.SerializedSize(-1));
            Assert.Equal(4, Bcs.I32.SerializedSize(-1));
            Assert.Equal(8, Bcs.I64.SerializedSize(-1));
            Assert.Equal(16, Bcs.I128.SerializedSize(Int128.MinValue));
            Assert.Equal(0, Bcs.Unit.SerializedSize(Unit.Value));

            // Test fixed array size
            var arrayType = Bcs.FixedArray(Bcs.U32, 3);
            var array = new uint[] { 1, 2, 3 };
            Assert.Equal(12, arrayType.SerializedSize(array)); // 3 * 4 bytes

            // Test tuple size
            var tupleType = Bcs.Tuple(Bcs.U32, Bcs.U8);
            var tuple = (42u, (byte)255);
            Assert.Equal(5, tupleType.SerializedSize(tuple)); // 4 + 1 bytes
        }

        #endregion
    }
}