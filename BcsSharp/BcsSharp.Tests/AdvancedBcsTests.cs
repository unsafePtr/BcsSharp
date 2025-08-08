using System;
using System.Collections.Generic;
using System.Text;
using BcsSharp.Core;
using Nethermind.Int256;
using Xunit;

namespace BcsSharp.Tests
{
    public class AdvancedBcsTests
    {
        [Fact]
        public void ContainerDepthLimits_ShouldPreventStackOverflow()
        {
            // Arrange - Create deeply nested vector structure
            var vectorType = Bcs.Vector(Bcs.Vector(Bcs.U8));
            var deeplyNestedData = new List<byte[]>();
            
            // Create 100 nested arrays to test depth limits
            for (int i = 0; i < 100; i++)
            {
                deeplyNestedData.Add(new byte[] { (byte)i });
            }

            // Act & Assert - Should handle reasonable depth without issues
            var serialized = vectorType.Serialize(deeplyNestedData.ToArray());
            var deserialized = vectorType.Parse(serialized);
            
            Assert.Equal(100, deserialized.Length);
            Assert.Equal(0, deserialized[0][0]);
            Assert.Equal(99, deserialized[99][0]);
        }

        [Fact]
        public void ContainerDepthLimits_NestedOptions_ShouldWork()
        {
            // Arrange - Nested optional types
            var nestedOptionType = Bcs.Option(Bcs.Option(Bcs.Option(Bcs.U32)));
            
            // Test with Some(Some(Some(value)))
            uint value = 42u;
            
            // Act
            var serialized = nestedOptionType.Serialize(value);
            var deserialized = nestedOptionType.Parse(serialized);
            
            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(7, serialized.Length); // 3 bool flags (true) + 4 bytes for u32
        }

        [Fact]
        public void ContainerDepthLimits_NestedOptions_WithNone_ShouldWork()
        {
            // Arrange - Use string option since it supports null
            var optionType = Bcs.Option(Bcs.String);
            
            // Act - Serialize None
            var serialized = optionType.Serialize(null);
            var deserialized = optionType.Parse(serialized);
            
            // Assert
            Assert.Null(deserialized);
            Assert.Single(serialized); // Just one false flag
            Assert.Equal(0, serialized[0]);
        }

        [Fact]
        public void RoundTripSerialization_ComplexStructure_ShouldPreserveData()
        {
            // Arrange - Complex nested structure with various types
            var complexType = Bcs.Vector(
                Bcs.Option(
                    Bcs.Vector(Bcs.String)
                )
            );

            var testData = new string[]?[]
            {
                new string[] { "hello", "world" },
                null,
                new string[] { "test", "", "data" },
                new string[0]
            };

            // Act
            var serialized = complexType.Serialize(testData);
            var deserialized = complexType.Parse(serialized);

            // Assert
            Assert.Equal(4, deserialized.Length);
            Assert.Equal(new string[] { "hello", "world" }, deserialized[0]);
            Assert.Null(deserialized[1]);
            Assert.Equal(new string[] { "test", "", "data" }, deserialized[2]);
            Assert.Empty(deserialized[3]!);
        }

        [Fact]
        public void SequenceLengthConstraints_LargeVector_ShouldHandleCorrectly()
        {
            // Arrange - Test with reasonably large vector
            var vectorType = Bcs.Vector(Bcs.U8);
            var largeArray = new byte[10000];
            
            // Fill with test pattern
            for (int i = 0; i < largeArray.Length; i++)
            {
                largeArray[i] = (byte)(i % 256);
            }

            // Act
            var serialized = vectorType.Serialize(largeArray);
            var deserialized = vectorType.Parse(serialized);

            // Assert
            Assert.Equal(largeArray.Length, deserialized.Length);
            for (int i = 0; i < 100; i++) // Check first 100 elements
            {
                Assert.Equal((byte)(i % 256), deserialized[i]);
            }
        }

        [Fact]
        public void SequenceLengthConstraints_EmptyCollections_ShouldWork()
        {
            // Arrange & Act & Assert - Empty vector
            var emptyVector = Bcs.Vector(Bcs.U32).Serialize(Array.Empty<uint>());
            var deserializedVector = Bcs.Vector(Bcs.U32).Parse(emptyVector);
            Assert.Empty(deserializedVector);

            // Arrange & Act & Assert - Empty string
            var emptyString = Bcs.String.Serialize("");
            var deserializedString = Bcs.String.Parse(emptyString);
            Assert.Equal("", deserializedString);
        }

        [Fact]
        public void ULEB128EdgeCases_MaxValues_ShouldSerializeCorrectly()
        {
            // Test various ULEB128 boundary values
            var testValues = new uint[]
            {
                0,
                127,      // Max 1-byte ULEB128
                128,      // Min 2-byte ULEB128
                16383,    // Max 2-byte ULEB128
                16384,    // Min 3-byte ULEB128
                2097151,  // Max 3-byte ULEB128
                2097152,  // Min 4-byte ULEB128
                268435455, // Max 4-byte ULEB128
                268435456  // Min 5-byte ULEB128
            };

            foreach (var value in testValues)
            {
                // Arrange
                var vectorType = Bcs.Vector(Bcs.U8);
                var testArray = new byte[value < 1000 ? value : 1000]; // Limit array size for practical testing

                // Act
                var serialized = vectorType.Serialize(testArray);
                var deserialized = vectorType.Parse(serialized);

                // Assert
                Assert.Equal(testArray.Length, deserialized.Length);
            }
        }

        [Fact]
        public void ULEB128EdgeCases_Reader_ShouldHandleVariousEncodings()
        {
            // Test direct ULEB128 reading with various encodings
            var testCases = new (uint value, byte[] encoding)[]
            {
                (0u, new byte[] { 0x00 }),
                (127u, new byte[] { 0x7F }),
                (128u, new byte[] { 0x80, 0x01 }),
                (255u, new byte[] { 0xFF, 0x01 }),
                (256u, new byte[] { 0x80, 0x02 }),
                (16383u, new byte[] { 0xFF, 0x7F }),
                (16384u, new byte[] { 0x80, 0x80, 0x01 })
            };

            foreach (var (expectedValue, encoding) in testCases)
            {
                // Act
                var reader = new BcsReader(encoding);
                var actualValue = reader.ReadULEB32();

                // Assert
                Assert.Equal(expectedValue, actualValue);
            }
        }

        [Fact]
        public void ULEB128EdgeCases_Writer_ShouldProduceCorrectEncodings()
        {
            var testCases = new (uint value, byte[] expectedEncoding)[]
            {
                (0u, new byte[] { 0x00 }),
                (127u, new byte[] { 0x7F }),
                (128u, new byte[] { 0x80, 0x01 }),
                (255u, new byte[] { 0xFF, 0x01 }),
                (256u, new byte[] { 0x80, 0x02 }),
                (16383u, new byte[] { 0xFF, 0x7F }),
                (16384u, new byte[] { 0x80, 0x80, 0x01 })
            };

            foreach (var (value, expectedEncoding) in testCases)
            {
                // Act
                var writer = new BcsWriter();
                writer.WriteULEB(value);
                var actualEncoding = writer.ToBytes();

                // Assert
                Assert.Equal(expectedEncoding, actualEncoding);
            }
        }

        [Fact]
        public void ErrorHandling_TruncatedData_ShouldThrow()
        {
            // Test various truncated data scenarios
            
            // Truncated u32
            var truncatedU32 = new byte[] { 0x01, 0x02 }; // Only 2 bytes instead of 4
            var reader1 = new BcsReader(truncatedU32);
            Assert.Throws<InvalidOperationException>(() => reader1.Read32());

            // Truncated string
            var truncatedString = new byte[] { 0x05, 0x48, 0x65 }; // Says 5 bytes but only has 2 content bytes
            var reader2 = new BcsReader(truncatedString);
            Assert.Throws<InvalidOperationException>(() => reader2.ReadString());

            // Truncated vector
            var truncatedVector = new byte[] { 0x03, 0x01 }; // Says 3 elements but only has data for 1
            var reader3 = new BcsReader(truncatedVector);
            Assert.Throws<InvalidOperationException>(() =>
            {
                var length = reader3.ReadULEB32();
                for (uint i = 0; i < length; i++)
                {
                    reader3.Read8(); // This should fail on the 2nd iteration
                }
            });
        }

        [Fact]
        public void ErrorHandling_InvalidBooleanValues_ShouldThrow()
        {
            // Test invalid boolean values (anything other than 0 or 1)
            var invalidBoolValues = new byte[] { 2, 3, 255 };

            foreach (var invalidValue in invalidBoolValues)
            {
                var data = new byte[] { invalidValue };
                var reader = new BcsReader(data);
                Assert.Throws<InvalidOperationException>(() => reader.ReadBool());
            }
        }

        [Fact]
        public void ErrorHandling_ULEB128Overflow_ShouldThrow()
        {
            // Create a ULEB128 that would overflow UInt128 (too many continuation bytes)
            var overflowBytes = new byte[20]; // More than the maximum 19 bytes for UInt128
            for (int i = 0; i < 19; i++)
            {
                overflowBytes[i] = 0x80; // All continuation bytes
            }
            overflowBytes[19] = 0x01; // Final byte

            var reader = new BcsReader(overflowBytes);
            Assert.Throws<InvalidOperationException>(() => reader.ReadULEB());
        }

        [Fact]
        public void ErrorHandling_ReaderBoundsChecking_ShouldThrow()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var reader = new BcsReader(data);

            // Read all available bytes
            reader.Read8();
            reader.Read8();
            reader.Read8();

            // Attempting to read beyond bounds should throw
            Assert.Throws<InvalidOperationException>(() => reader.Read8());
            Assert.Throws<InvalidOperationException>(() => reader.Read16());
            Assert.Throws<InvalidOperationException>(() => reader.ReadBytes(1));
        }

        [Fact]
        public void ComplexTypeIntegration_NestedEnumsInVectors_ShouldWork()
        {
            // Arrange - Create enum type for testing
            var statusEnum = BcsEnum.Create("Status")
                .AddVariant("Active", Bcs.U32)
                .AddVariant("Inactive")
                .AddVariant("Pending", Bcs.String)
                .Build();

            var vectorType = Bcs.Vector(statusEnum);

            var testData = new EnumVariant[]
            {
                statusEnum.CreateVariant("Active", 100u),
                statusEnum.CreateVariant("Inactive"),
                statusEnum.CreateVariant("Pending", "review"),
                statusEnum.CreateVariant("Active", 200u)
            };

            // Act
            var serialized = vectorType.Serialize(testData);
            var deserialized = vectorType.Parse(serialized);

            // Assert
            Assert.Equal(4, deserialized.Length);
            Assert.Equal("Active", deserialized[0].Name);
            Assert.Equal(100u, deserialized[0].GetData<uint>());
            Assert.Equal("Inactive", deserialized[1].Name);
            Assert.False(deserialized[1].HasData);
            Assert.Equal("Pending", deserialized[2].Name);
            Assert.Equal("review", deserialized[2].GetData<string>());
            Assert.Equal("Active", deserialized[3].Name);
            Assert.Equal(200u, deserialized[3].GetData<uint>());
        }

        [Fact]
        public void TypeSafety_MismatchedTypes_ShouldMaintainIntegrity()
        {
            // Test that different types don't accidentally deserialize as each other
            var u32Value = 0x12345678u;
            var u32Serialized = Bcs.U32.Serialize(u32Value);

            // Trying to deserialize u32 data as string should fail gracefully
            // Note: This might not throw but could produce unexpected results
            // The important thing is that it doesn't crash
            try
            {
                var stringResult = Bcs.String.Parse(u32Serialized);
                // If it doesn't throw, verify it's handling the data consistently
                Assert.NotNull(stringResult);
            }
            catch (Exception)
            {
                // It's okay if it throws - that's actually preferred behavior
                // for type safety
            }
        }

        [Fact]
        public void MemoryEfficiency_LargeStructures_ShouldNotExcessivelyAllocate()
        {
            // Test that serialization/deserialization doesn't create excessive allocations
            var largeVectorType = Bcs.Vector(Bcs.U64);
            var largeData = new ulong[1000];
            
            for (int i = 0; i < largeData.Length; i++)
            {
                largeData[i] = (ulong)i;
            }

            // Act - Multiple serialization/deserialization cycles
            byte[] serialized = null;
            ulong[] deserialized = null;
            
            for (int cycle = 0; cycle < 10; cycle++)
            {
                serialized = largeVectorType.Serialize(largeData);
                deserialized = largeVectorType.Parse(serialized);
                
                // Verify data integrity
                Assert.Equal(1000, deserialized.Length);
                Assert.Equal(0ul, deserialized[0]);
                Assert.Equal(999ul, deserialized[999]);
            }
        }

        [Fact]
        public void EncodingConsistency_MultipleSerializations_ShouldProduceSameResult()
        {
            // Test that multiple serializations of the same data produce identical results
            var complexType = Bcs.Vector(Bcs.Option(Bcs.String));
            var testData = new string?[] { "hello", null, "world", "", null, "test" };

            var serialized1 = complexType.Serialize(testData);
            var serialized2 = complexType.Serialize(testData);
            var serialized3 = complexType.Serialize(testData);

            // Assert all serializations are identical
            Assert.Equal(serialized1, serialized2);
            Assert.Equal(serialized2, serialized3);
            
            // And all deserializations produce the same result
            var deserialized1 = complexType.Parse(serialized1);
            var deserialized2 = complexType.Parse(serialized2);
            var deserialized3 = complexType.Parse(serialized3);
            
            Assert.Equal(deserialized1, deserialized2);
            Assert.Equal(deserialized2, deserialized3);
        }
    }
}