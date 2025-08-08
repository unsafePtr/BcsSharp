using System;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Nethermind.Int256;
using Xunit;

namespace BcsSharp.Tests
{
    public class BasicTypesTests
    {
        [Fact]
        public void U8Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new U8Type();
            byte value = 123;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Single(serialized);
            Assert.Equal(123, serialized[0]);
        }

        [Fact]
        public void U16Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new U16Type();
            ushort value = 0x1234;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(2, serialized.Length);
            Assert.Equal(new byte[] { 0x34, 0x12 }, serialized); // Little-endian
        }

        [Fact]
        public void U32Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new U32Type();
            uint value = 0x12345678;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(4, serialized.Length);
            Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, serialized); // Little-endian
        }

        [Fact]
        public void U64Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new U64Type();
            ulong value = 0x123456789ABCDEF0;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(8, serialized.Length);
        }

        [Fact]
        public void BoolType_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new BoolType();

            // Act & Assert - True
            var trueBytes = type.Serialize(true);
            var trueResult = type.Parse(trueBytes);
            Assert.True(trueResult);
            Assert.Single(trueBytes);
            Assert.Equal(1, trueBytes[0]);

            // Act & Assert - False
            var falseBytes = type.Serialize(false);
            var falseResult = type.Parse(falseBytes);
            Assert.False(falseResult);
            Assert.Single(falseBytes);
            Assert.Equal(0, falseBytes[0]);
        }

        [Fact]
        public void StringType_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new StringType();
            string value = "Hello, World!";

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(14, serialized.Length); // 1 byte length + 13 bytes content
            Assert.Equal(13, serialized[0]); // Length prefix
        }

        [Fact]
        public void StringType_ShouldHandleEmptyString()
        {
            // Arrange
            var type = new StringType();
            string value = "";

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Single(serialized);
            Assert.Equal(0, serialized[0]); // Length prefix
        }

        [Fact]
        public void VectorType_ShouldSerializeAndDeserializeIntArray()
        {
            // Arrange
            var elementType = new U32Type();
            var vectorType = new VectorType<uint>(elementType);
            uint[] value = { 1, 2, 3, 4, 5 };

            // Act
            var serialized = vectorType.Serialize(value);
            var deserialized = vectorType.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(21, serialized.Length); // 1 byte length + 5*4 bytes elements
            Assert.Equal(5, serialized[0]); // Length prefix
        }

        [Fact]
        public void VectorType_ShouldHandleEmptyArray()
        {
            // Arrange
            var elementType = new U8Type();
            var vectorType = new VectorType<byte>(elementType);
            byte[] value = Array.Empty<byte>();

            // Act
            var serialized = vectorType.Serialize(value);
            var deserialized = vectorType.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Single(serialized);
            Assert.Equal(0, serialized[0]); // Length prefix
        }

        [Fact]
        public void OptionType_ShouldSerializeAndDeserializeSome()
        {
            // Arrange
            var innerType = new U32Type();
            var optionType = new OptionType<uint>(innerType);
            uint value = 42;

            // Act
            var serialized = optionType.Serialize(value);
            var deserialized = optionType.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(5, serialized.Length); // 1 byte flag + 4 bytes value
            Assert.Equal(1, serialized[0]); // Some flag
        }

        [Fact]
        public void OptionType_ShouldSerializeAndDeserializeNone()
        {
            // Arrange
            var innerType = new StringType();
            var optionType = new OptionType<string>(innerType);
            string? value = null;

            // Act
            var serialized = optionType.Serialize(value);
            var deserialized = optionType.Parse(serialized);

            // Assert
            Assert.Null(deserialized);
            Assert.Single(serialized);
            Assert.Equal(0, serialized[0]); // None flag
        }

        [Fact]
        public void U128Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new U128Type();
            UInt128 value = new UInt128(0x123456789ABCDEF0, 0x0FEDCBA987654321);

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(16, serialized.Length);
        }

        [Fact]
        public void U256Type_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var type = new U256Type();
            UInt256 value = new UInt256(0x12345678); // Simple test value

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(32, serialized.Length);
        }

        [Fact]
        public void U256Type_ShouldHandleZero()
        {
            // Arrange
            var type = new U256Type();
            UInt256 value = UInt256.Zero;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(32, serialized.Length);
            Assert.All(serialized, b => Assert.Equal(0, b));
        }

        [Fact]
        public void U256Type_ShouldHandleMaxValue()
        {
            // Arrange
            var type = new U256Type();
            UInt256 value = UInt256.MaxValue;

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(32, serialized.Length);
            Assert.All(serialized, b => Assert.Equal(0xFF, b));
        }

        [Fact]
        public void U256Type_ShouldHandleLargeValues()
        {
            // Arrange
            var type = new U256Type();
            var value = UInt256.Parse("123456789ABCDEF0FEDCBA9876543210123456789ABCDEF0FEDCBA9876543210", System.Globalization.NumberStyles.HexNumber);

            // Act
            var serialized = type.Serialize(value);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(value, deserialized);
            Assert.Equal(32, serialized.Length);
        }

        [Fact]
        public void U256Type_ShouldHaveCorrectSerializedSize()
        {
            // Arrange
            var type = new U256Type();
            var value = new UInt256(42);

            // Act
            var size = type.SerializedSize(value);

            // Assert
            Assert.Equal(32, size);
        }

        [Fact]
        public void U128Type_ShouldHandleMaxValue()
        {
            // Arrange
            var type = new U128Type();
            UInt128 maxValue = UInt128.MaxValue;

            // Act
            var serialized = type.Serialize(maxValue);
            var deserialized = type.Parse(serialized);

            // Assert
            Assert.Equal(maxValue, deserialized);
            Assert.Equal(16, serialized.Length);
        }

        [Fact]
        public void SerializedSize_ShouldReturnCorrectSizes()
        {
            // Assert
            Assert.Equal(1, Bcs.U8.SerializedSize(123));
            Assert.Equal(2, Bcs.U16.SerializedSize(123));
            Assert.Equal(4, Bcs.U32.SerializedSize(123));
            Assert.Equal(8, Bcs.U64.SerializedSize(123));
            Assert.Equal(16, Bcs.U128.SerializedSize(new UInt128(123, 0)));
            Assert.Equal(32, Bcs.U256.SerializedSize(new UInt256(123)));
            Assert.Equal(1, Bcs.Bool.SerializedSize(true));
        }

        [Fact]
        public void FromHex_ShouldDeserializeFromHexString()
        {
            // Arrange
            var type = Bcs.U32;
            uint value = 0x12345678;

            // Act
            var serialized = type.Serialize(value);
            var hex = Convert.ToHexString(serialized).ToLowerInvariant();
            var deserialized = type.FromHex(hex);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void FromBase64_ShouldDeserializeFromBase64String()
        {
            // Arrange
            var type = Bcs.String;
            string value = "Hello";

            // Act
            var serialized = type.Serialize(value);
            var base64 = Convert.ToBase64String(serialized);
            var deserialized = type.FromBase64(base64);

            // Assert
            Assert.Equal(value, deserialized);
        }
    }
}