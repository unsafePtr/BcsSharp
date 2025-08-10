using System;
using BcsSharp.Core;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tests for simple C-style enum serialization (integer-backed enums)
    /// </summary>
    public class SimpleEnumTests
    {
        #region Test Enums

        public enum Status : byte
        {
            Pending = 0,
            Active = 1,
            Disabled = 2
        }

        public enum Priority : int
        {
            Low = 100,
            Medium = 200,
            High = 300,
            Critical = 999
        }

        public enum Size : ushort
        {
            Small = 10,
            Medium = 100,
            Large = 1000,
            XLarge = 10000
        }

        #endregion

        [Fact]
        public void SimpleEnum_ByteBacked_ShouldSerializeAsInteger()
        {
            // Arrange
            var status = Status.Active;

            // Act
            var serialized = BcsSerializer.Serialize(status);
            var deserialized = BcsSerializer.Deserialize<Status>(serialized);

            // Assert
            Assert.Single(serialized); // Should be 1 byte
            Assert.Equal(1, serialized[0]); // Status.Active = 1
            Assert.Equal(status, deserialized);
        }

        [Fact]
        public void SimpleEnum_IntBacked_ShouldSerializeAsInteger()
        {
            // Arrange
            var priority = Priority.High;

            // Act
            var serialized = BcsSerializer.Serialize(priority);
            var deserialized = BcsSerializer.Deserialize<Priority>(serialized);

            // Assert
            Assert.Single(serialized); // Should be 1 byte
            Assert.Equal(priority, deserialized);
        }

        [Fact]
        public void SimpleEnum_UShortBacked_ShouldSerializeAsInteger()
        {
            // Arrange
            var size = Size.Large;

            // Act
            var serialized = BcsSerializer.Serialize(size);
            var deserialized = BcsSerializer.Deserialize<Size>(serialized);

            // Assert
            Assert.Single(serialized); // Should be 1 byte
            Assert.Equal(size, deserialized);
        }

        [Theory]
        [InlineData(Status.Pending, (byte)0)]
        [InlineData(Status.Active, (byte)1)]
        [InlineData(Status.Disabled, (byte)2)]
        public void SimpleEnum_AllStatusValues_ShouldRoundTrip(Status status, byte expectedValue)
        {
            // Act
            var serialized = BcsSerializer.Serialize(status);
            var deserialized = BcsSerializer.Deserialize<Status>(serialized);

            // Assert
            Assert.Equal(status, deserialized);
            Assert.Single(serialized);
            Assert.Equal(expectedValue, serialized[0]);
        }

        [Fact]
        public void SimpleEnum_DeterministicSerialization_ShouldBeConsistent()
        {
            // Arrange
            var priority = Priority.Critical;

            // Act
            var serialized1 = BcsSerializer.Serialize(priority);
            var serialized2 = BcsSerializer.Serialize(priority);
            var serialized3 = BcsSerializer.Serialize(priority);

            // Assert
            Assert.Equal(serialized1, serialized2);
            Assert.Equal(serialized2, serialized3);
        }
    }
}