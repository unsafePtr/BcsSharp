using System;
using System.Collections.Generic;
using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tests for basic BCS formatters (Unit, Tuple, Map)
    /// </summary>
    public class BasicFormatterTests
    {
        [Fact]
        public void Unit_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var unit = Unit.Value;
            
            // Act
            var serialized = BcsSerializer.Serialize(unit);
            var deserialized = BcsSerializer.Deserialize<Unit>(serialized);
            
            // Assert
            Assert.Equal(unit, deserialized);
            Assert.Empty(serialized); // Unit serializes to 0 bytes
        }
        
        [Fact]
        public void Unit_Properties_ShouldWork()
        {
            // Arrange
            var unit1 = Unit.Value;
            var unit2 = new Unit();
            
            // Assert
            Assert.Equal(unit1, unit2);
            Assert.True(unit1 == unit2);
            Assert.False(unit1 != unit2);
            Assert.Equal("()", unit1.ToString());
            Assert.Equal(0, unit1.GetHashCode());
        }
        
        [Theory]
        [InlineData("hello", 42)]
        [InlineData("", 0)]
        [InlineData("world", -1)]
        public void Tuple2_SerializeDeserialize_ShouldRoundTrip(string item1, int item2)
        {
            // Arrange
            var tuple = (item1, item2);
            
            // Act
            var serialized = BcsSerializer.Serialize(tuple);
            var deserialized = BcsSerializer.Deserialize<(string, int)>(serialized);
            
            // Assert
            Assert.Equal(tuple.Item1, deserialized.Item1);
            Assert.Equal(tuple.Item2, deserialized.Item2);
        }
        
        [Fact]
        public void Tuple3_SerializeDeserialize_ShouldRoundTrip()
        {
            // Arrange
            var tuple = ("test", 123, true);
            
            // Act
            var serialized = BcsSerializer.Serialize(tuple);
            var deserialized = BcsSerializer.Deserialize<(string, int, bool)>(serialized);
            
            // Assert
            Assert.Equal(tuple.Item1, deserialized.Item1);
            Assert.Equal(tuple.Item2, deserialized.Item2);
            Assert.Equal(tuple.Item3, deserialized.Item3);
        }
        
        [Fact]
        public void Tuple4_SerializeDeserialize_ShouldRoundTrip()
        {
            // Arrange
            var tuple = ("test", 123, true, (byte)255);
            
            // Act
            var serialized = BcsSerializer.Serialize(tuple);
            var deserialized = BcsSerializer.Deserialize<(string, int, bool, byte)>(serialized);
            
            // Assert
            Assert.Equal(tuple.Item1, deserialized.Item1);
            Assert.Equal(tuple.Item2, deserialized.Item2);
            Assert.Equal(tuple.Item3, deserialized.Item3);
            Assert.Equal(tuple.Item4, deserialized.Item4);
        }
        
        [Fact]
        public void Map_SerializeDeserialize_ShouldRoundTrip()
        {
            // Arrange
            var map = new Dictionary<string, int>
            {
                ["zebra"] = 3,
                ["alpha"] = 1,
                ["beta"] = 2
            };
            
            // Act
            var serialized = BcsSerializer.Serialize(map);
            var deserialized = BcsSerializer.Deserialize<Dictionary<string, int>>(serialized);
            
            // Assert
            Assert.Equal(map.Count, deserialized.Count);
            foreach (var kvp in map)
            {
                Assert.True(deserialized.ContainsKey(kvp.Key));
                Assert.Equal(kvp.Value, deserialized[kvp.Key]);
            }
        }
        
        [Fact]
        public void Map_SerializedOrder_ShouldBeSorted()
        {
            // Arrange
            var map = new Dictionary<string, int>
            {
                ["zebra"] = 3,
                ["alpha"] = 1,
                ["beta"] = 2
            };
            
            // Act
            var serialized = BcsSerializer.Serialize(map);
            var reader = new BcsReader(serialized);
            
            // Assert - Read and verify key order
            var count = reader.ReadULEB32();
            Assert.Equal(3u, count);
            
            var key1 = reader.ReadString();
            reader.ReadI32(); // skip value
            var key2 = reader.ReadString();
            reader.ReadI32(); // skip value  
            var key3 = reader.ReadString();
            
            // BCS sorts by lexicographical order of serialized bytes (including length prefix)
            // "beta" (4 bytes): [04] + ... comes before "alpha" (5 bytes): [05] + ...
            Assert.Equal("beta", key1);   // 04... < 05...
            Assert.Equal("alpha", key2);  // 05 61... < 05 7A...
            Assert.Equal("zebra", key3);
        }
        
        [Fact]
        public void Map_EmptyMap_ShouldWork()
        {
            // Arrange
            var emptyMap = new Dictionary<string, int>();
            
            // Act
            var serialized = BcsSerializer.Serialize(emptyMap);
            var deserialized = BcsSerializer.Deserialize<Dictionary<string, int>>(serialized);
            
            // Assert
            Assert.Empty(deserialized);
            Assert.Single(serialized); // Just the length byte (0)
        }
        
        [Fact]
        public void Map_DuplicateKeys_ShouldThrowOnDeserialize()
        {
            // Arrange - Manually create invalid data with duplicate keys
            var writer = new BcsWriter(new BcsWriterOptions());
            writer.WriteULEB(2u); // 2 entries
            writer.WriteString("key");
            writer.Write(100);
            writer.WriteString("key"); // Duplicate key
            writer.Write(200);
            
            var invalidData = writer.ToBytes();
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                BcsSerializer.Deserialize<Dictionary<string, int>>(invalidData));
        }
        
        [Fact]
        public void Map_UnsortedKeys_ShouldThrowOnDeserialize()
        {
            // Arrange - Manually create invalid data with unsorted keys
            var writer = new BcsWriter(new BcsWriterOptions());
            writer.WriteULEB(2u); // 2 entries
            writer.WriteString("zebra"); // Should come after "alpha"
            writer.Write(100);
            writer.WriteString("alpha");
            writer.Write(200);
            
            var invalidData = writer.ToBytes();
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                BcsSerializer.Deserialize<Dictionary<string, int>>(invalidData));
        }
    }
}