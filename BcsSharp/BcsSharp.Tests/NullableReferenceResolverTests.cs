using System;
using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tests for NullableReferenceResolver functionality with nullable reference types
    /// </summary>
    public class NullableReferenceResolverTests
    {
        private readonly NullableReferenceResolver _resolver = NullableReferenceResolver.Instance;

        [Fact]
        public void NullableReferenceResolver_ShouldReturnNull_ForValueTypes()
        {
            // Act & Assert - Value types should return null
            Assert.Null(_resolver.GetFormatter<int>());
            Assert.Null(_resolver.GetFormatter<byte>());
            Assert.Null(_resolver.GetFormatter<bool>());
            
            // Nullable value types should also return null (handled by NullableResolver)
            Assert.Null(_resolver.GetFormatter<int?>());
            Assert.Null(_resolver.GetFormatter<bool?>());
        }

        [Fact]
        public void NullableReferenceResolver_ShouldReturnFormatter_ForStringType()
        {
            // Act
            var formatter = _resolver.GetFormatter<string>();

            // Assert - String should get a nullable reference formatter
            Assert.NotNull(formatter);
            Assert.Same(NullableReferenceResolver.StringNullableFormatter, formatter);
        }

        [Fact]
        public void NullableReferenceResolver_ShouldUseCachedFormatter_ForCommonTypes()
        {
            // Act - Get string formatter multiple times
            var formatter1 = _resolver.GetFormatter<string>();
            var formatter2 = _resolver.GetFormatter<string>();

            // Assert - Should return the same cached instance
            Assert.Same(formatter1, formatter2);
            Assert.Same(NullableReferenceResolver.StringNullableFormatter, formatter1);
        }

        [Theory]
        [InlineData("Hello World")]
        [InlineData("")]
        [InlineData(null)]
        public void NullableString_SerializeDeserialize_ShouldRoundTrip(string? value)
        {
            // Arrange
            var formatter = _resolver.GetFormatter<string>();
            Assert.NotNull(formatter);

            // Act
            var serialized = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<string?>(serialized);

            // Assert
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void NullableString_SerializedSize_ShouldBeCorrect()
        {
            // Arrange
            var formatter = _resolver.GetFormatter<string>();
            Assert.NotNull(formatter);

            // Act & Assert - Null string should be 1 byte (discriminant only)
            var nullSize = formatter.GetSerializedSize(null);
            Assert.Equal(1, nullSize);

            // Act & Assert - Non-null string should be discriminant + ULEB length + string bytes
            var testString = "test";
            var valueSize = formatter.GetSerializedSize(testString);
            Assert.Equal(6, valueSize); // 1 (discriminant) + 1 (length) + 4 (UTF-8 bytes)
        }

        [Fact]
        public void NullableReferenceResolver_ThreadSafety_ShouldWork()
        {
            // Arrange - Multiple threads requesting same formatter type
            var tasks = new System.Threading.Tasks.Task<IBcsFormatter<string>?>[10];

            // Act - Concurrent access to resolver
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() => _resolver.GetFormatter<string>());
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

        [Fact]
        public void NullableReferenceFormatter_BcsOptionFormat_ShouldUseCorrectDiscriminants()
        {
            // Arrange
            var writer1 = new BcsWriter(new BcsWriterOptions());
            var writer2 = new BcsWriter(new BcsWriterOptions());
            var formatter = NullableReferenceResolver.StringNullableFormatter;

            // Act - Serialize null and non-null values
            formatter.Serialize(ref writer1, null);
            formatter.Serialize(ref writer2, "test");

            var nullBytes = writer1.ToBytes();
            var valueBytes = writer2.ToBytes();

            // Assert - Null should start with discriminant 0, non-null with discriminant 1
            Assert.Equal(0, nullBytes[0]); // None discriminant
            Assert.Equal(1, nullBytes.Length); // Only discriminant for null

            Assert.Equal(1, valueBytes[0]); // Some discriminant
            Assert.True(valueBytes.Length > 1); // Discriminant + value data
        }

        [Fact]
        public void NullableReferenceFormatter_Deserialize_ShouldHandleInvalidDiscriminants()
        {
            // Arrange
            var invalidData = new byte[] { 2 }; // Invalid discriminant (should be 0 or 1)
            var reader = new BcsReader(invalidData);
            var formatter = NullableReferenceResolver.StringNullableFormatter;

            // Act & Assert - Should throw for invalid discriminant
            var threwException = false;
            try
            {
                formatter.Deserialize(ref reader);
            }
            catch (InvalidOperationException)
            {
                threwException = true;
            }
            Assert.True(threwException);
        }
    }
}