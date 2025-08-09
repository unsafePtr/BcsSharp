using System;
using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tests for NullableResolver functionality with nullable value types (T?)
    /// </summary>
    public class NullableResolverTests
    {
        private readonly NullableResolver _resolver = NullableResolver.Instance;

        [Fact]
        public void NullableResolver_ShouldReturnNull_ForNonNullableTypes()
        {
            // Act & Assert - Non-nullable value types should return null
            Assert.Null(_resolver.GetFormatter<int>());
            Assert.Null(_resolver.GetFormatter<byte>());
            Assert.Null(_resolver.GetFormatter<bool>());
            Assert.Null(_resolver.GetFormatter<string>()); // Reference type
        }

        [Fact]
        public void NullableResolver_ShouldReturnFormatter_ForNullableValueTypes()
        {
            // Act & Assert - Nullable value types should return formatters
            Assert.NotNull(_resolver.GetFormatter<int?>());
            Assert.NotNull(_resolver.GetFormatter<byte?>());
            Assert.NotNull(_resolver.GetFormatter<bool?>());
            Assert.NotNull(_resolver.GetFormatter<long?>());
            Assert.NotNull(_resolver.GetFormatter<UInt128?>());
        }

        [Fact]
        public void NullableResolver_ShouldUseCachedFormatters_ForCommonTypes()
        {
            // Act - Get formatters for common types
            var intFormatter1 = _resolver.GetFormatter<int?>();
            var intFormatter2 = _resolver.GetFormatter<int?>();
            var byteFormatter1 = _resolver.GetFormatter<byte?>();
            var byteFormatter2 = _resolver.GetFormatter<byte?>();

            // Assert - Same instances should be returned (cached)
            Assert.Same(intFormatter1, intFormatter2);
            Assert.Same(byteFormatter1, byteFormatter2);
            
            // Assert - Should be the cached instances from OptionFormatterCache
            Assert.Same(OptionFormatterCache.Int32OptionFormatter, intFormatter1);
            Assert.Same(OptionFormatterCache.ByteOptionFormatter, byteFormatter1);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(null)]
        public void NullableInt_SerializeDeserialize_ShouldRoundTrip(int? value)
        {
            // Arrange
            var formatter = _resolver.GetFormatter<int?>();
            Assert.NotNull(formatter);

            // Act
            var serialized = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<int?>(serialized);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Theory]
        [InlineData((byte)255)]
        [InlineData(null)]
        public void NullableByte_SerializeDeserialize_ShouldRoundTrip(byte? value)
        {
            // Arrange
            var formatter = _resolver.GetFormatter<byte?>();
            Assert.NotNull(formatter);

            // Act
            var serialized = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<byte?>(serialized);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void NullableBool_SerializeDeserialize_ShouldRoundTrip(bool? value)
        {
            // Arrange
            var formatter = _resolver.GetFormatter<bool?>();
            Assert.NotNull(formatter);

            // Act
            var serialized = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<bool?>(serialized);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void NullableUInt128_SerializeDeserialize_ShouldRoundTrip()
        {
            // Arrange
            var formatter = _resolver.GetFormatter<UInt128?>();
            Assert.NotNull(formatter);

            UInt128? testValue = UInt128.MaxValue;
            UInt128? nullValue = null;

            // Act & Assert - Test with value
            var serialized1 = BcsSerializer.Serialize(testValue);
            var deserialized1 = BcsSerializer.Deserialize<UInt128?>(serialized1);
            Assert.Equal(testValue, deserialized1);

            // Act & Assert - Test with null
            var serialized2 = BcsSerializer.Serialize(nullValue);
            var deserialized2 = BcsSerializer.Deserialize<UInt128?>(serialized2);
            Assert.Equal(nullValue, deserialized2);
        }

        [Fact]
        public void NullableValue_SerializedSize_ShouldBeCorrect()
        {
            // Arrange
            var formatter = _resolver.GetFormatter<int?>();
            Assert.NotNull(formatter);

            // Act & Assert - Null value should be 1 byte (discriminant only)
            var nullSize = formatter.GetSerializedSize(null);
            Assert.Equal(1, nullSize);

            // Act & Assert - Non-null value should be discriminant + value size
            var valueSize = formatter.GetSerializedSize(42);
            Assert.Equal(5, valueSize); // 1 (discriminant) + 4 (int32)
        }

        [Fact]
        public void NullableResolver_ShouldReturnNull_ForUncommonTypes()
        {
            // Arrange - Type without a formatter (DateTime doesn't have a BCS formatter)
            var formatter = _resolver.GetFormatter<DateTime?>();

            // Act & Assert - Should return null since DateTime doesn't have a formatter in StandardResolver
            Assert.Null(formatter);
        }

        [Fact]
        public void NullableResolver_ThreadSafety_ShouldWork()
        {
            // Arrange - Multiple threads requesting same formatter type
            var tasks = new System.Threading.Tasks.Task<IBcsFormatter<int?>?>[10];

            // Act - Concurrent access to resolver
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() => _resolver.GetFormatter<int?>());
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert - All should return the same cached instance
            var firstFormatter = tasks[0].Result;
            Assert.NotNull(firstFormatter);

            for (int i = 1; i < tasks.Length; i++)
            {
                Assert.Same(firstFormatter, tasks[i].Result);
            }
        }
    }
}