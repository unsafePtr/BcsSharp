using System;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    public class DebugNestedTests
    {
        public class SimpleClass
        {
            public string Name { get; set; } = string.Empty;
            public uint Value { get; set; }
        }

        public class ContainerClass
        {
            public string Title { get; set; } = string.Empty;
            public SimpleClass Nested { get; set; } = new();
        }

        [Fact]
        public void Debug_SimpleNestedClass_ShouldWork()
        {
            // Arrange
            var container = new ContainerClass
            {
                Title = "Test",
                Nested = new SimpleClass { Name = "NestedTest", Value = 42 }
            };

            // Act
            var containerType = BcsStruct.Create<ContainerClass>();
            var serialized = containerType.Serialize(container);
            var deserialized = containerType.Parse(serialized);

            // Assert
            Assert.Equal(container.Title, deserialized.Title);
            Assert.Equal(container.Nested.Name, deserialized.Nested.Name);
            Assert.Equal(container.Nested.Value, deserialized.Nested.Value);
        }

        [Fact]
        public void Debug_SimpleClassArray_ShouldWork()
        {
            // Test arrays of custom classes
            var container = new SimpleClass[]
            {
                new() { Name = "First", Value = 1 },
                new() { Name = "Second", Value = 2 }
            };

            // Act
            var arrayType = Bcs.Vector(BcsStruct.Create<SimpleClass>());
            var serialized = arrayType.Serialize(container);
            var deserialized = arrayType.Parse(serialized);

            // Assert
            Assert.Equal(2, deserialized.Length);
            Assert.Equal("First", deserialized[0].Name);
            Assert.Equal(1u, deserialized[0].Value);
            Assert.Equal("Second", deserialized[1].Name);
            Assert.Equal(2u, deserialized[1].Value);
        }

        public class ArrayContainer
        {
            public string Title { get; set; } = string.Empty;
            public SimpleClass[] Items { get; set; } = [];
        }

        [Fact]
        public void Debug_NestedArrayOfClasses_ShouldWork()
        {
            // This simulates the GameAsset.Attributes issue
            var container = new ArrayContainer
            {
                Title = "Container",
                Items = new[]
                {
                    new SimpleClass { Name = "Item1", Value = 10 },
                    new SimpleClass { Name = "Item2", Value = 20 }
                }
            };

            // Act
            var containerType = BcsStruct.Create<ArrayContainer>();
            var serialized = containerType.Serialize(container);
            var deserialized = containerType.Parse(serialized);

            // Assert
            Assert.Equal(container.Title, deserialized.Title);
            Assert.Equal(2, deserialized.Items.Length);
            Assert.Equal("Item1", deserialized.Items[0].Name);
            Assert.Equal(10u, deserialized.Items[0].Value);
        }
    }
}