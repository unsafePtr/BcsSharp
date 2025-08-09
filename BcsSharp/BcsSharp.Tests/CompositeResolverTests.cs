using System;
using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tests for CompositeResolver functionality and resolver chaining
    /// </summary>
    public class CompositeResolverTests
    {
        [Fact]
        public void CompositeResolver_Default_ShouldHaveCorrectResolverChain()
        {
            // Arrange
            var resolver = CompositeResolver.Default;

            // Act & Assert - Should resolve nullable value types via NullableResolver
            var nullableIntFormatter = resolver.GetFormatter<int?>();
            Assert.NotNull(nullableIntFormatter);
            Assert.Same(OptionFormatterCache.Int32OptionFormatter, nullableIntFormatter);

            // Act & Assert - Should resolve nullable reference types via NullableReferenceResolver
            var nullableStringFormatter = resolver.GetFormatter<string>();
            Assert.NotNull(nullableStringFormatter);
            Assert.Same(NullableReferenceResolver.StringNullableFormatter, nullableStringFormatter);

            // Act & Assert - Should resolve standard types via StandardResolver
            var intFormatter = resolver.GetFormatter<int>();
            Assert.NotNull(intFormatter);
            Assert.Same(Int32Formatter.Instance, intFormatter);
        }

        [Fact]
        public void CompositeResolver_Create_ShouldUseStandardResolverFirst()
        {
            // Arrange - Create resolver with standard resolver first
            var customResolver = new TestCustomResolver();
            var resolver = CompositeResolver.Create(customResolver);

            // Act - Get formatter for type that both resolvers can handle
            var formatter = resolver.GetFormatter<int>();

            // Assert - Should get from StandardResolver, not custom
            Assert.NotNull(formatter);
            Assert.Same(Int32Formatter.Instance, formatter);
        }

        [Fact]
        public void CompositeResolver_CreateWithDefaults_ShouldIncludeAllDefaults()
        {
            // Arrange
            var customResolver = new TestCustomResolver();
            var resolver = CompositeResolver.CreateWithDefaults(customResolver);

            // Act & Assert - Should handle nullable types (from defaults)
            Assert.NotNull(resolver.GetFormatter<int?>());
            Assert.NotNull(resolver.GetFormatter<string>());
            
            // Act & Assert - Should handle standard types (from defaults)
            Assert.NotNull(resolver.GetFormatter<int>());
            
            // Act & Assert - Should handle custom types (from custom resolver)
            Assert.NotNull(resolver.GetFormatter<TestCustomType>());
        }

        [Fact]
        public void CompositeResolver_ShouldCacheFormatters()
        {
            // Arrange
            var resolver = CompositeResolver.Default;

            // Act - Get same formatter multiple times
            var formatter1 = resolver.GetFormatter<int>();
            var formatter2 = resolver.GetFormatter<int>();

            // Assert - Should return the same cached instance
            Assert.Same(formatter1, formatter2);
        }

        // NOTE: Removed test for unsupported types since custom type formatters 
        // will be handled by SourceGenerator in the future

        [Theory]
        [InlineData(42)]
        [InlineData(null)]
        public void CompositeResolver_NullableIntegration_ShouldWork(int? value)
        {
            // Arrange - Use default composite resolver
            var originalDefault = BcsSerializer.DefaultResolver;
            BcsSerializer.DefaultResolver = CompositeResolver.Default;

            try
            {
                // Act - Serialize/deserialize nullable int
                var serialized = BcsSerializer.Serialize(value);
                var deserialized = BcsSerializer.Deserialize<int?>(serialized);

                // Assert
                Assert.Equal(value, deserialized);
            }
            finally
            {
                BcsSerializer.DefaultResolver = originalDefault;
            }
        }

        [Theory]
        [InlineData("Hello World")]
        [InlineData(null)]
        public void CompositeResolver_NullableStringIntegration_ShouldWork(string? value)
        {
            // Arrange - Use default composite resolver
            var originalDefault = BcsSerializer.DefaultResolver;
            BcsSerializer.DefaultResolver = CompositeResolver.Default;

            try
            {
                // Act - Serialize/deserialize nullable string
                var serialized = BcsSerializer.Serialize(value);
                var deserialized = BcsSerializer.Deserialize<string?>(serialized);

                // Assert
                Assert.Equal(value, deserialized);
            }
            finally
            {
                BcsSerializer.DefaultResolver = originalDefault;
            }
        }

        [Fact]
        public void CompositeResolver_ArraysAndLists_ShouldWork()
        {
            // Arrange
            var resolver = CompositeResolver.Default;
            var intArray = new int[] { 1, 2, 3, 4, 5 };
            var nullableIntArray = new int?[] { 1, null, 3 };

            // Act & Assert - Regular arrays
            var arrayFormatter = resolver.GetFormatter<int[]>();
            Assert.NotNull(arrayFormatter);

            var serializedArray = BcsSerializer.Serialize(intArray, resolver);
            var deserializedArray = BcsSerializer.Deserialize<int[]>(serializedArray, resolver);
            Assert.Equal(intArray, deserializedArray);

            // Act & Assert - Arrays of nullable types
            var nullableArrayFormatter = resolver.GetFormatter<int?[]>();
            Assert.NotNull(nullableArrayFormatter);

            var serializedNullableArray = BcsSerializer.Serialize(nullableIntArray, resolver);
            var deserializedNullableArray = BcsSerializer.Deserialize<int?[]>(serializedNullableArray, resolver);
            Assert.Equal(nullableIntArray, deserializedNullableArray);
        }

        [Fact]
        public void CompositeResolver_ThreadSafety_ShouldWork()
        {
            // Arrange
            var resolver = CompositeResolver.Default;
            var tasks = new System.Threading.Tasks.Task<IBcsFormatter<int>?>[20];

            // Act - Concurrent access from multiple threads
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() => resolver.GetFormatter<int>());
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

        #region Test Helper Classes

        public class TestCustomType
        {
            public int Value { get; set; }
        }

        public class TestCustomTypeFormatter : IBcsFormatter<TestCustomType>
        {
            public void Serialize(ref BcsWriter writer, TestCustomType value)
            {
                writer.Write(value?.Value ?? 0);
            }

            public TestCustomType Deserialize(ref BcsReader reader)
            {
                return new TestCustomType { Value = (int)reader.Read32() };
            }

            public int? GetSerializedSize(TestCustomType value)
            {
                return 4;
            }
        }

        public class TestCustomIntFormatter : IBcsFormatter<int>
        {
            public void Serialize(ref BcsWriter writer, int value)
            {
                writer.Write(value);
            }

            public int Deserialize(ref BcsReader reader)
            {
                return (int)reader.Read32();
            }

            public int? GetSerializedSize(int value)
            {
                return 4;
            }
        }

        public class TestCustomResolver : IFormatterResolver
        {
            public IBcsFormatter<T>? GetFormatter<T>()
            {
                if (typeof(T) == typeof(TestCustomType))
                    return (IBcsFormatter<T>)(object)new TestCustomTypeFormatter();
                
                return null;
            }
        }

        public class TestOverrideResolver : IFormatterResolver
        {
            public IBcsFormatter<T>? GetFormatter<T>()
            {
                if (typeof(T) == typeof(int))
                    return (IBcsFormatter<T>)(object)new TestCustomIntFormatter();
                
                return null;
            }
        }

        #endregion
    }
}