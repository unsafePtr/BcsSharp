using System;
using System.IO;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tuple serialization tests to match Rust tuple examples
    /// In C#, we represent tuples as structs with indexed fields to match BCS serialization order
    /// </summary>
    public class TupleSerializationTests
    {
        #region C# Enums (same as before)

        public enum AssetType : byte
        {
            Weapon = 0,
            Armor = 1,
            Consumable = 2,
            Material = 3,
            Currency = 4
        }

        public enum Rarity : byte
        {
            Common = 0,
            Uncommon = 1,
            Rare = 2,
            Epic = 3,
            Legendary = 4
        }

        public enum TransactionType : byte
        {
            Transfer = 0,
            Purchase = 1,
            Sale = 2,
            Mint = 3,
            Burn = 4
        }

        #endregion

        #region C# Structs (same as before)

        public class User
        {
            public ulong Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public ulong Balance { get; set; }
            public bool IsVerified { get; set; }
        }

        public class Attribute
        {
            public string Name { get; set; } = string.Empty;
            public uint Value { get; set; }
        }

        public class GameAsset
        {
            public string AssetId { get; set; } = string.Empty;
            public AssetType AssetType { get; set; }
            public Rarity Rarity { get; set; }
            public byte Level { get; set; }
            public Attribute[] Attributes { get; set; } = [];
        }

        public class Transaction
        {
            public string TxId { get; set; } = string.Empty;
            public ulong FromUser { get; set; }
            public ulong ToUser { get; set; }
            public ulong Amount { get; set; }
            public ulong Timestamp { get; set; }
            public TransactionType TxType { get; set; }
        }

        #endregion

        #region Tuple Representation Structures

        /// <summary>
        /// Represents (String, u32) tuple
        /// </summary>
        public class SimplePair
        {
            public string Item1 { get; set; } = string.Empty;
            public uint Item2 { get; set; }
        }

        /// <summary>
        /// Represents (u64, bool, String) tuple
        /// </summary>
        public class Triple
        {
            public ulong Item1 { get; set; }
            public bool Item2 { get; set; }
            public string Item3 { get; set; } = string.Empty;
        }

        /// <summary>
        /// Represents (String, GameAsset) tuple
        /// </summary>
        public class NestedTuple
        {
            public string Item1 { get; set; } = string.Empty;
            public GameAsset Item2 { get; set; } = new();
        }

        /// <summary>
        /// Represents (String, Vec<u32>) tuple
        /// </summary>
        public class TupleWithArray
        {
            public string Item1 { get; set; } = string.Empty;
            public uint[] Item2 { get; set; } = [];
        }

        /// <summary>
        /// Represents (User, Transaction, bool) tuple
        /// </summary>
        public class ComplexTuple
        {
            public User Item1 { get; set; } = new();
            public Transaction Item2 { get; set; } = new();
            public bool Item3 { get; set; }
        }

        /// <summary>
        /// Main structure containing all tuple examples (matches Rust TupleExamples)
        /// </summary>
        public class TupleExamples
        {
            public SimplePair SimplePair { get; set; } = new();
            public Triple Triple { get; set; } = new();
            public NestedTuple NestedTuple { get; set; } = new();
            public TupleWithArray TupleWithArray { get; set; } = new();
            public ComplexTuple ComplexTuple { get; set; } = new();
        }

        #endregion

        [Fact]
        public void TupleSerialization_SimplePair_ShouldWork()
        {
            // Arrange - Simple tuple (String, u32)
            var simplePair = new SimplePair
            {
                Item1 = "hello",
                Item2 = 42
            };

            // Act
            var pairType = BcsStruct.Create<SimplePair>();
            var serialized = pairType.Serialize(simplePair);
            var deserialized = pairType.Parse(serialized);

            // Assert
            Assert.Equal(simplePair.Item1, deserialized.Item1);
            Assert.Equal(simplePair.Item2, deserialized.Item2);
        }

        [Fact]
        public void TupleSerialization_Triple_ShouldWork()
        {
            // Arrange - Triple (u64, bool, String)
            var triple = new Triple
            {
                Item1 = 123,
                Item2 = true,
                Item3 = "world"
            };

            // Act
            var tripleType = BcsStruct.Create<Triple>();
            var serialized = tripleType.Serialize(triple);
            var deserialized = tripleType.Parse(serialized);

            // Assert
            Assert.Equal(triple.Item1, deserialized.Item1);
            Assert.Equal(triple.Item2, deserialized.Item2);
            Assert.Equal(triple.Item3, deserialized.Item3);
        }

        [Fact]
        public void TupleSerialization_NestedTuple_ShouldWork()
        {
            // Arrange - Nested tuple (String, GameAsset)
            var gameAsset = new GameAsset
            {
                AssetId = "sword_001",
                AssetType = AssetType.Weapon,
                Rarity = Rarity.Epic,
                Level = 15,
                Attributes = new[]
                {
                    new Attribute { Name = "damage", Value = 85 },
                    new Attribute { Name = "speed", Value = 12 }
                }
            };

            var nestedTuple = new NestedTuple
            {
                Item1 = "asset_ref",
                Item2 = gameAsset
            };

            // Act
            var nestedType = BcsStruct.Create<NestedTuple>();
            var serialized = nestedType.Serialize(nestedTuple);
            var deserialized = nestedType.Parse(serialized);

            // Assert
            Assert.Equal(nestedTuple.Item1, deserialized.Item1);
            Assert.Equal(nestedTuple.Item2.AssetId, deserialized.Item2.AssetId);
            Assert.Equal(nestedTuple.Item2.AssetType, deserialized.Item2.AssetType);
            Assert.Equal(nestedTuple.Item2.Rarity, deserialized.Item2.Rarity);
            Assert.Equal(nestedTuple.Item2.Level, deserialized.Item2.Level);
            Assert.Equal(2, deserialized.Item2.Attributes.Length);
        }

        [Fact]
        public void TupleSerialization_CompleteExample_ByteByByteComparison()
        {
            // Arrange - Create the complete TupleExamples structure matching Rust
            var user = new User
            {
                Id = 12345,
                Name = "Alice",
                Email = "alice@example.com",
                Balance = 1000,
                IsVerified = true
            };

            var gameAsset = new GameAsset
            {
                AssetId = "sword_001",
                AssetType = AssetType.Weapon,
                Rarity = Rarity.Epic,
                Level = 15,
                Attributes = new[]
                {
                    new Attribute { Name = "damage", Value = 85 },
                    new Attribute { Name = "speed", Value = 12 }
                }
            };

            var transaction = new Transaction
            {
                TxId = "0x1234567890abcdef",
                FromUser = 12345,
                ToUser = 67890,
                Amount = 250,
                Timestamp = 1640995200,
                TxType = TransactionType.Transfer
            };

            var tupleExamples = new TupleExamples
            {
                SimplePair = new SimplePair { Item1 = "hello", Item2 = 42 },
                Triple = new Triple { Item1 = 123, Item2 = true, Item3 = "world" },
                NestedTuple = new NestedTuple { Item1 = "asset_ref", Item2 = gameAsset },
                TupleWithArray = new TupleWithArray { Item1 = "numbers", Item2 = new uint[] { 1, 2, 3, 4, 5 } },
                ComplexTuple = new ComplexTuple { Item1 = user, Item2 = transaction, Item3 = false }
            };

            // Act
            var tupleType = BcsStruct.Create<TupleExamples>();
            var csharpBytes = tupleType.Serialize(tupleExamples);

            // Read Rust bytes
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
            {
                currentDir = currentDir.Parent;
            }
            
            if (currentDir == null)
                throw new DirectoryNotFoundException("Could not find solution directory");
                
            var rustFilesPath = Path.Combine(currentDir.Parent!.FullName, "rust-sui-bcs-test");
            var rustBytes = File.ReadAllBytes(Path.Combine(rustFilesPath, "tuples.bcs"));

            // Assert - byte-perfect compatibility

            // Perfect match confirmed - enable assertions
            Assert.Equal(rustBytes.Length, csharpBytes.Length);
            Assert.Equal(rustBytes, csharpBytes);
        }

        [Fact]
        public void TupleSerialization_ShowRustReference()
        {
            // Verify Rust tuple serialization file exists and is readable
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
            {
                currentDir = currentDir.Parent;
            }
            
            if (currentDir == null)
                throw new DirectoryNotFoundException("Could not find solution directory");
                
            var rustFilesPath = Path.Combine(currentDir.Parent!.FullName, "rust-sui-bcs-test");
            var tupleBytes = File.ReadAllBytes(Path.Combine(rustFilesPath, "tuples.bcs"));

            Assert.True(tupleBytes.Length > 0);
            Assert.Equal(193, tupleBytes.Length); // Expected tuple example size
        }
    }
}