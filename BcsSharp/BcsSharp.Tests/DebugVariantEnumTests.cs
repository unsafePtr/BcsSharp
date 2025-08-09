using System;
using System.Reflection;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Debug tests for Rust-style variant enum serialization
    /// </summary>
    public class DebugVariantEnumTests
    {
        #region Simple Test Enum for Debugging

        /// <summary>
        /// Base interface for the debug enum
        /// </summary>
        [BcsEnum]
        public interface IDebugEnum { }

        /// <summary>
        /// Simple unit variant
        /// </summary>
        [BcsEnumVariant(0)]
        public class UnitVariant : IDebugEnum
        {
            public UnitVariant() { }
        }

        /// <summary>
        /// Variant with string data
        /// </summary>
        [BcsEnumVariant(1)]
        public class StringVariant : IDebugEnum
        {
            [BcsEnumData(0)]
            public string Value { get; set; } = "";

            public StringVariant() { }
            public StringVariant(string value) { Value = value; }
        }

        #endregion

        [Fact]
        public void Debug_CheckEnumResolver_CanFindFormatter()
        {
            // This test will help us debug what's happening with the EnumResolver
            var resolver = BcsSharp.Core.Resolvers.EnumResolver.Instance;
            
            // Act - Try to get formatter for IDebugEnum
            var formatter = resolver.GetFormatter<IDebugEnum>();
            
            // Assert - Should find a formatter
            Assert.NotNull(formatter);
            Assert.Equal(typeof(IDebugEnum), ((BcsSharp.Core.IBcsFormatter)formatter).TargetType);
        }

        [Fact]
        public void Debug_SimpleUnitVariant_ShouldWork()
        {
            // Arrange
            var variant = new UnitVariant();

            try
            {
                // Act
                var serialized = BcsSerializer.Serialize<IDebugEnum>(variant);
                var deserialized = BcsSerializer.Deserialize<IDebugEnum>(serialized);

                // Assert
                Assert.IsType<UnitVariant>(deserialized);
                Assert.Single(serialized); // Should be just the variant index
                Assert.Equal(0, serialized[0]); // Variant index 0
            }
            catch (Exception ex)
            {
                // Debug info
                Assert.True(false, $"Serialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Fact]
        public void Debug_CheckBcsEnumAttribute()
        {
            // Check if the BcsEnum attribute is properly applied
            var attribute = typeof(IDebugEnum).GetCustomAttribute<BcsEnumAttribute>();
            Assert.NotNull(attribute);
        }

        [Fact]
        public void Debug_CheckVariantAttributes()
        {
            // Check if variant attributes are properly applied
            var unitAttr = typeof(UnitVariant).GetCustomAttribute<BcsEnumVariantAttribute>();
            var stringAttr = typeof(StringVariant).GetCustomAttribute<BcsEnumVariantAttribute>();
            
            Assert.NotNull(unitAttr);
            Assert.Equal(0u, unitAttr.Index);
            
            Assert.NotNull(stringAttr);
            Assert.Equal(1u, stringAttr.Index);
        }

        [Fact]
        public void Debug_CheckTypeHierarchy()
        {
            // Check if the type hierarchy is correct
            Assert.True(typeof(IDebugEnum).IsAssignableFrom(typeof(UnitVariant)));
            Assert.True(typeof(IDebugEnum).IsAssignableFrom(typeof(StringVariant)));
            
            // Check if variants implement the interface
            IDebugEnum unit = new UnitVariant();
            IDebugEnum str = new StringVariant("test");
            
            Assert.NotNull(unit);
            Assert.NotNull(str);
        }
    }
}