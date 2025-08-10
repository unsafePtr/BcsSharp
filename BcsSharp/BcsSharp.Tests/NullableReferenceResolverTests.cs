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
    }
}