using System;
using System.Numerics;
using BcsSharp.Core;
using Nethermind.Int256;
using Xunit;

namespace BcsSharp.Tests
{
    public class BcsReaderTests
    {
        [Fact]
        public void Read8_ShouldReadSingleByte()
        {
            // Arrange
            var data = new byte[] { 0xFF, 0x00, 0x42 };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Equal(0xFF, reader.Read8());
            Assert.Equal(0x00, reader.Read8());
            Assert.Equal(0x42, reader.Read8());
        }

        [Fact]
        public void Read16_ShouldReadUShortInLittleEndian()
        {
            // Arrange
            var data = new byte[] { 0x34, 0x12 }; // 0x1234 in little-endian
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read16();

            // Assert
            Assert.Equal(0x1234, result);
        }

        [Fact]
        public void Read32_ShouldReadUIntInLittleEndian()
        {
            // Arrange
            var data = new byte[] { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 in little-endian
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read32();

            // Assert
            Assert.Equal(0x12345678u, result);
        }

        [Fact]
        public void Read64_ShouldReadULongInLittleEndian()
        {
            // Arrange
            var data = new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 };
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read64();

            // Assert
            Assert.Equal(0x0123456789ABCDEFul, result);
        }

        [Fact]
        public void ReadBool_ShouldReadBooleanValues()
        {
            // Arrange
            var data = new byte[] { 0x00, 0x01 };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.False(reader.ReadBool());
            Assert.True(reader.ReadBool());
        }

        [Fact]
        public void ReadBool_ShouldThrowOnInvalidValue()
        {
            // Arrange
            var data = new byte[] { 0x02 };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => reader.ReadBool());
        }

        [Fact]
        public void ReadULEB_ShouldReadVariableLengthIntegers()
        {
            // Arrange - ULEB128 encoding of various numbers
            var data = new byte[] {
                0x00,       // 0
                0x7F,       // 127
                0x80, 0x01, // 128
                0xFF, 0x01, // 255
                0x80, 0x02  // 256
            };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Equal(UInt128.Zero, reader.ReadULEB());
            Assert.Equal(new UInt128(0, 127), reader.ReadULEB());
            Assert.Equal(new UInt128(0, 128), reader.ReadULEB());
            Assert.Equal(new UInt128(0, 255), reader.ReadULEB());
            Assert.Equal(new UInt128(0, 256), reader.ReadULEB());
        }

        [Fact]
        public void ReadString_ShouldReadLengthPrefixedString()
        {
            // Arrange - "Hello" as length-prefixed UTF-8
            var data = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
            var reader = new BcsReader(data);

            // Act
            var result = reader.ReadString();

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ReadBytes_ShouldReadSpecifiedNumberOfBytes()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var reader = new BcsReader(data);

            // Act
            var result = reader.ReadBytes(3);

            // Assert
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result);
            Assert.Equal(2, reader.RemainingBytes);
        }

        [Fact]
        public void Read8_ShouldThrowWhenNotEnoughBytes()
        {
            // Arrange
            var data = new byte[] { };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => reader.Read8());
        }

        [Fact]
        public void RemainingBytes_ShouldReturnCorrectCount()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Equal(3, reader.RemainingBytes);
            reader.Read8();
            Assert.Equal(2, reader.RemainingBytes);
            reader.Read8();
            Assert.Equal(1, reader.RemainingBytes);
            reader.Read8();
            Assert.Equal(0, reader.RemainingBytes);
        }

        [Fact]
        public void HasRemainingBytes_ShouldReturnCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0x01 };
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.True(reader.HasRemainingBytes);
            reader.Read8();
            Assert.False(reader.HasRemainingBytes);
        }

        [Fact]
        public void Reset_ShouldResetPosition()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var reader = new BcsReader(data);

            // Act
            reader.Read8();
            reader.Read8();
            Assert.Equal(1, reader.RemainingBytes);

            reader.Reset();

            // Assert
            Assert.Equal(3, reader.RemainingBytes);
            Assert.Equal(0x01, reader.Read8());
        }

        [Fact]
        public void Read128_ShouldReadUInt128InLittleEndian()
        {
            // Arrange - little-endian bytes for UInt128 value
            var data = new byte[] {
                0x21, 0x43, 0x65, 0x87, 0xA9, 0xCB, 0xED, 0x0F, // low 64 bits
                0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12  // high 64 bits
            };
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read128();

            // Assert
            var expected = new UInt128(0x123456789ABCDEF0, 0x0FEDCBA987654321);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Read256_ShouldReadUInt256InLittleEndian()
        {
            // Arrange - little-endian bytes for UInt256 value 0x12345678
            var data = new byte[32];
            data[0] = 0x78;
            data[1] = 0x56;
            data[2] = 0x34;
            data[3] = 0x12;
            // Rest are zeros

            var reader = new BcsReader(data);

            // Act
            var result = reader.Read256();

            // Assert
            var expected = new UInt256(0x12345678);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Read256_ShouldReadZero()
        {
            // Arrange - all zeros
            var data = new byte[32]; // All zeros by default
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read256();

            // Assert
            Assert.Equal(UInt256.Zero, result);
        }

        [Fact]
        public void Read256_ShouldReadMaxValue()
        {
            // Arrange - all 0xFF bytes
            var data = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                data[i] = 0xFF;
            }
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read256();

            // Assert
            Assert.Equal(UInt256.MaxValue, result);
        }

        [Fact]
        public void Read256_ShouldReadOne()
        {
            // Arrange - first byte is 1, rest are zeros
            var data = new byte[32];
            data[0] = 0x01;
            // Rest are zeros by default
            var reader = new BcsReader(data);

            // Act
            var result = reader.Read256();

            // Assert
            Assert.Equal(UInt256.One, result);
        }

        [Fact]
        public void Read256_ShouldReadLargeValue()
        {
            // Arrange - create a large value in little-endian format
            var data = new byte[32];
            // Fill with pattern 0x123456789ABCDEF0... in little-endian
            data[0] = 0xF0;
            data[1] = 0xDE;
            data[2] = 0xBC;
            data[3] = 0x9A;
            data[4] = 0x78;
            data[5] = 0x56;
            data[6] = 0x34;
            data[7] = 0x12;
            data[8] = 0xF0;
            data[9] = 0xDE;
            data[10] = 0xBC;
            data[11] = 0x9A;
            data[12] = 0x78;
            data[13] = 0x56;
            data[14] = 0x34;
            data[15] = 0x12;
            data[16] = 0xF0;
            data[17] = 0xDE;
            data[18] = 0xBC;
            data[19] = 0x9A;
            data[20] = 0x78;
            data[21] = 0x56;
            data[22] = 0x34;
            data[23] = 0x12;
            data[24] = 0xF0;
            data[25] = 0xDE;
            data[26] = 0xBC;
            data[27] = 0x9A;
            data[28] = 0x78;
            data[29] = 0x56;
            data[30] = 0x34;
            data[31] = 0x12;

            var reader = new BcsReader(data);

            // Act
            var result = reader.Read256();

            // Assert
            var expected = UInt256.Parse("123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0", System.Globalization.NumberStyles.HexNumber);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Read256_ShouldThrowWhenNotEnoughBytes()
        {
            // Arrange - only 31 bytes instead of 32
            var data = new byte[31];
            var reader = new BcsReader(data);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => reader.Read256());
        }

        [Fact]
        public void Read256_ShouldHandleMultipleValues()
        {
            // Arrange - two UInt256 values back to back
            var data = new byte[64]; // 32 * 2
            // First value: 0x12345678
            data[0] = 0x78;
            data[1] = 0x56;
            data[2] = 0x34;
            data[3] = 0x12;
            // Rest of first value are zeros (indices 4-31)

            // Second value: 0xABCDEF01
            data[32] = 0x01;
            data[33] = 0xEF;
            data[34] = 0xCD;
            data[35] = 0xAB;
            // Rest of second value are zeros (indices 36-63)

            var reader = new BcsReader(data);

            // Act
            var first = reader.Read256();
            var second = reader.Read256();

            // Assert
            Assert.Equal(new UInt256(0x12345678), first);
            Assert.Equal(new UInt256(0xABCDEF01), second);
        }
    }
}