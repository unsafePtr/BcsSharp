using BcsSharp.Core;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Tests for CompositeResolver functionality and resolver chaining
/// </summary>
public class CompositeResolverTests
{
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

    [Fact]
    public void CompositeResolver_Lists_ShouldWork()
    {
        // Arrange
        var resolver = CompositeResolver.Default;
        var intArray = new List<int> { 1, 2, 3, 4, 5 };
        var nullableIntArray = new List<int?> { 1, null, 3 };

        // Act & Assert - Regular arrays
        var arrayFormatter = resolver.GetFormatter<List<int>>();
        Assert.NotNull(arrayFormatter);

        var serializedArray = BcsSerializer.Serialize(intArray, resolver);
        var deserializedArray = BcsSerializer.Deserialize<List<int>>(serializedArray, resolver);
        Assert.Equal(intArray, deserializedArray);

        // Act & Assert - Arrays of nullable types
        var nullableArrayFormatter = resolver.GetFormatter<List<int?>>();
        Assert.NotNull(nullableArrayFormatter);

        var serializedNullableArray = BcsSerializer.Serialize(nullableIntArray, resolver);
        var deserializedNullableArray = BcsSerializer.Deserialize<List<int?>>(serializedNullableArray, resolver);
        Assert.Equal(nullableIntArray, deserializedNullableArray);
    }

    [Fact]
    public async Task CompositeResolver_ThreadSafety_ShouldWork()
    {
        // Arrange
        var resolver = CompositeResolver.Default;
        var tasks = new Task<IBcsFormatter<int>?>[20];

        // Act - Concurrent access from multiple threads
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(resolver.GetFormatter<int>);
        }

        await Task.WhenAll(tasks);

        // Assert - All should return the same cached instance
        var firstFormatter = await tasks[0];
        Assert.NotNull(firstFormatter);

        for (var i = 1; i < tasks.Length; i++)
        {
            Assert.Same(firstFormatter, await tasks[i]);
        }
    }

    public class TestCustomType
    {
        public int Value { get; set; }
    }
}
