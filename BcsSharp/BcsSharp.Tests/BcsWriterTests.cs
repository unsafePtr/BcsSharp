using System;
using System.Buffers;
using System.Numerics;
using BcsSharp.Core;
using Nethermind.Int256;
using Xunit;

namespace BcsSharp.Tests
{
    public class BcsWriterTests
    {
        [Fact]
        public void Write8_ShouldWriteSingleByte()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.Write((byte)0xFF).Write8(0x00).Write8(0x42);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0xFF, 0x00, 0x42 }, result);
        }

        [Fact]
        public void Write16_ShouldWriteUShortInLittleEndian()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.Write((ushort)0x1234);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0x34, 0x12 }, result); // Little-endian
        }

        [Fact]
        public void Write32_ShouldWriteUIntInLittleEndian()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.Write(0x12345678u);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, result); // Little-endian
        }

        [Fact]
        public void Write64_ShouldWriteULongInLittleEndian()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.Write(0x0123456789ABCDEFul);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }, result);
        }

        [Fact]
        public void WriteBool_ShouldWriteBooleanValues()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.WriteBool(false).WriteBool(true);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0x00, 0x01 }, result);
        }

        [Fact]
        public void WriteULEB_ShouldWriteVariableLengthIntegers()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.WriteULEB(0u)          // 0
                  .WriteULEB(127u)        // 127
                  .WriteULEB(128u)        // 128
                  .WriteULEB(255u)        // 255
                  .WriteULEB(256u);       // 256

            var result = writer.ToBytes();

            // Assert
            var expected = new byte[] {
                0x00,       // 0
                0x7F,       // 127
                0x80, 0x01, // 128
                0xFF, 0x01, // 255
                0x80, 0x02  // 256
            };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WriteULEB_ShouldWriteUInt128Values()
        {
            // Arrange
            var writer = new BcsWriter();
            var bigValue = new UInt128(0, 300);

            // Act
            writer.WriteULEB(bigValue);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0xAC, 0x02 }, result); // 300 in ULEB128
        }

        [Fact]
        public void WriteString_ShouldWriteLengthPrefixedString()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            writer.WriteString("Hello");
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
        }

        [Fact]
        public void WriteBytes_ShouldWriteRawBytes()
        {
            // Arrange
            var writer = new BcsWriter();
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            writer.WriteBytes(data);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(data, result);
        }

        [Fact]
        public void ToHex_ShouldReturnHexString()
        {
            // Arrange
            var writer = new BcsWriter();
            writer.Write((byte)0xFF).Write8(0x00).Write8(0xAB);

            // Act
            var result = writer.ToHex();

            // Assert
            Assert.Equal("ff00ab", result);
        }

        [Fact]
        public void ToBase64_ShouldReturnBase64String()
        {
            // Arrange
            var writer = new BcsWriter();
            writer.WriteString("Hello");

            // Act
            var result = writer.ToBase64();

            // Assert
            var expected = Convert.ToBase64String(new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F });
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Length_ShouldReturnCorrectLength()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act & Assert
            Assert.Equal(0, writer.Length);
            writer.Write((byte)0x01);
            Assert.Equal(1, writer.Length);
            writer.Write((uint)0x12345678);
            Assert.Equal(5, writer.Length);
        }

        [Fact]
        public void ChainedOperations_ShouldWork()
        {
            // Arrange
            var writer = new BcsWriter();

            // Act
            var result = writer
                .Write((byte)0x01)
                .Write16(0x0203)
                .Write32(0x04050607)
                .WriteBool(true)
                .WriteString("Hi")
                .ToBytes();

            // Assert
            var expected = new byte[] {
                0x01,               // u8
                0x03, 0x02,         // u16 little-endian
                0x07, 0x06, 0x05, 0x04, // u32 little-endian
                0x01,               // bool true
                0x02, 0x48, 0x69    // string "Hi"
            };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Write128_ShouldWriteUInt128()
        {
            // Arrange
            var writer = new BcsWriter();
            var value = new UInt128(0x123456789ABCDEF0, 0x0FEDCBA987654321);

            // Act
            writer.Write(value);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(16, result.Length); // u128 is 16 bytes

            // Verify little-endian format
            var expected = new byte[] {
                0x21, 0x43, 0x65, 0x87, 0xA9, 0xCB, 0xED, 0x0F, // low 64 bits
                0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12  // high 64 bits
            };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Write256_ShouldWriteUInt256()
        {
            // Arrange
            var writer = new BcsWriter();
            var value = new UInt256(0x12345678);

            // Act
            writer.Write(value);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(32, result.Length); // u256 is 32 bytes
            Assert.Equal(0x78, result[0]); // First byte should be LSB in little-endian
            Assert.Equal(0x56, result[1]);
            Assert.Equal(0x34, result[2]);
            Assert.Equal(0x12, result[3]);
        }

        [Fact]
        public void Write256_ShouldWriteZero()
        {
            // Arrange
            var writer = new BcsWriter();
            var value = UInt256.Zero;

            // Act
            writer.Write(value);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(32, result.Length);
            Assert.All(result, b => Assert.Equal(0, b)); // All bytes should be zero
        }

        [Fact]
        public void Write256_ShouldWriteMaxValue()
        {
            // Arrange
            var writer = new BcsWriter();
            var value = UInt256.MaxValue;

            // Act
            writer.Write(value);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(32, result.Length);
            Assert.All(result, b => Assert.Equal(0xFF, b)); // All bytes should be 0xFF
        }

        [Fact]
        public void Write256_ShouldWriteLargeValue()
        {
            // Arrange
            var writer = new BcsWriter();
            // Create a large value: 0x123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0
            var hexString = "123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0";
            var value = UInt256.Parse(hexString, System.Globalization.NumberStyles.HexNumber);

            // Act
            writer.Write(value);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(32, result.Length);
            // Verify little-endian format - last bytes of hex should be first in array
            Assert.Equal(0xF0, result[0]);
            Assert.Equal(0xDE, result[1]);
            Assert.Equal(0xBC, result[2]);
            Assert.Equal(0x9A, result[3]);
        }

        [Fact]
        public void Write256_ShouldWriteOne()
        {
            // Arrange
            var writer = new BcsWriter();
            var value = UInt256.One;

            // Act
            writer.Write(value);
            var result = writer.ToBytes();

            // Assert
            Assert.Equal(32, result.Length);
            Assert.Equal(0x01, result[0]); // First byte should be 1
            // All other bytes should be zero
            for (int i = 1; i < 32; i++)
            {
                Assert.Equal(0x00, result[i]);
            }
        }

        [Fact]
        public void Write256_ShouldHandleRoundTripSerialization()
        {
            // Arrange
            var writer = new BcsWriter();
            var originalValue = new UInt256(0xFEDCBA9876543210UL);

            // Act - Write and then read back
            writer.Write(originalValue);
            var serialized = writer.ToBytes();
            var reader = new BcsReader(serialized);
            var deserializedValue = reader.Read256();

            // Assert
            Assert.Equal(originalValue, deserializedValue);
        }

        [Fact]
        public void BcsWriter_ShouldWorkWithExternalBufferWriter()
        {
            // Arrange
            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new BcsWriter(bufferWriter);

            // Act
            writer.Write((byte)0x42)
                  .Write16(0x1234)
                  .Write32(0x56789ABC);

            // Assert
            var result = bufferWriter.WrittenSpan.ToArray();
            var expected = new byte[] { 0x42, 0x34, 0x12, 0xBC, 0x9A, 0x78, 0x56 };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void BcsWriter_ExternalBufferWriter_ShouldThrowOnUnavailableOperations()
        {
            // Arrange
            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new BcsWriter(bufferWriter);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => writer.Length);
            Assert.Throws<InvalidOperationException>(() => writer.Position);
            Assert.Throws<InvalidOperationException>(() => writer.ToBytes());
            Assert.Throws<InvalidOperationException>(() => writer.ToHex());
            Assert.Throws<InvalidOperationException>(() => writer.ToBase64());
            Assert.Throws<InvalidOperationException>(() => writer.Reset());
        }

        [Fact]
        public void BcsReader_ReadOnlyMemory_Constructor_ShouldWork()
        {
            // Arrange
            var data = new byte[] { 0x42, 0x34, 0x12, 0xBC, 0x9A, 0x78, 0x56 };
            var memory = data.AsMemory();
            var reader = new BcsReader(memory);

            // Act & Assert
            Assert.Equal(0x42, reader.Read8());
            Assert.Equal(0x1234, reader.Read16());
            Assert.Equal(0x56789ABCU, reader.Read32());
        }

        [Fact]
        public void BcsReader_ReadBytesAsSpan_ShouldWork()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var reader = new BcsReader(data);

            // Act
            var span = reader.ReadBytesAsSpan(3);

            // Assert
            Assert.Equal(3, span.Length);
            Assert.Equal(0x01, span[0]);
            Assert.Equal(0x02, span[1]);
            Assert.Equal(0x03, span[2]);
            Assert.Equal(3, reader.Position);
        }

        [Fact]
        public void BcsReader_ReadBytesToSpan_ShouldWork()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var reader = new BcsReader(data);
            Span<byte> destination = stackalloc byte[3];

            // Act
            reader.ReadBytes(destination);

            // Assert
            Assert.Equal(0x01, destination[0]);
            Assert.Equal(0x02, destination[1]);
            Assert.Equal(0x03, destination[2]);
            Assert.Equal(3, reader.Position);
        }

        [Fact]
        public void BcsReader_OptimizedMethods_ShouldMatchOriginalBehavior()
        {
            // Arrange
            var writer = new BcsWriter();
            writer.Write((byte)0x42)
                  .Write16(0x1234)
                  .Write32(0x56789ABC)
                  .Write64(0x123456789ABCDEF0)
                  .WriteString("Hello")
                  .WriteBool(true);

            var data = writer.ToBytes();
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Equal(0x42, reader.Read8());
            Assert.Equal(0x1234, reader.Read16());
            Assert.Equal(0x56789ABCU, reader.Read32());
            Assert.Equal(0x123456789ABCDEF0UL, reader.Read64());
            Assert.Equal("Hello", reader.ReadString());
            Assert.True(reader.ReadBool());
        }

    }
}