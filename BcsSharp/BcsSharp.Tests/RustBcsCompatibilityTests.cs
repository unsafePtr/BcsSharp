using System;
using System.Collections.Generic;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// C# equivalent of the Rust BCS example structs to test serialization compatibility
    /// </summary>
    public class RustBcsCompatibilityTests
    {
        #region Enums (C# equivalent of Rust enums)

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

        #region Structs (C# equivalent of Rust structs)

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

        public class MarketplaceItem
        {
            public string ItemId { get; set; } = string.Empty;
            public ulong Seller { get; set; }
            public GameAsset Asset { get; set; } = new();
            public ulong Price { get; set; }
            public ulong ListedAt { get; set; }
            public bool IsActive { get; set; }
        }

        public class SuiCompatibleData
        {
            public string Owner { get; set; } = string.Empty;
            public ulong Balance { get; set; }
            public byte[] Metadata { get; set; } = [];
        }

        #endregion

        [Fact]
        public void RustBcsCompatibility_UserStruct_ShouldSerialize()
        {
            // Arrange - Same data as Rust example
            var user = new User
            {
                Id = 12345,
                Name = "Alice",
                Email = "alice@example.com",
                Balance = 1000,
                IsVerified = true
            };

            // Act
            var userType = BcsStruct.Create<User>();
            var serialized = userType.Serialize(user);
            var deserialized = userType.Parse(serialized);

            // Assert
            Assert.Equal(user.Id, deserialized.Id);
            Assert.Equal(user.Name, deserialized.Name);
            Assert.Equal(user.Email, deserialized.Email);
            Assert.Equal(user.Balance, deserialized.Balance);
            Assert.Equal(user.IsVerified, deserialized.IsVerified);

            // Validate serialization worked
        }

        [Fact]
        public void RustBcsCompatibility_GameAssetStruct_ShouldSerialize()
        {
            // Arrange - Same data as Rust example
            var asset = new GameAsset
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

            // Act
            var assetType = BcsStruct.Create<GameAsset>();
            var serialized = assetType.Serialize(asset);
            var deserialized = assetType.Parse(serialized);

            // Assert
            Assert.Equal(asset.AssetId, deserialized.AssetId);
            Assert.Equal(asset.AssetType, deserialized.AssetType);
            Assert.Equal(asset.Rarity, deserialized.Rarity);
            Assert.Equal(asset.Level, deserialized.Level);
            Assert.Equal(2, deserialized.Attributes.Length);
            Assert.Equal("damage", deserialized.Attributes[0].Name);
            Assert.Equal(85u, deserialized.Attributes[0].Value);
            Assert.Equal("speed", deserialized.Attributes[1].Name);
            Assert.Equal(12u, deserialized.Attributes[1].Value);

            Console.WriteLine($"📋 GameAsset serialized:");
            Console.WriteLine($"   Size: {serialized.Length} bytes");
            Console.WriteLine($"   Hex:  {Convert.ToHexString(serialized[..Math.Min(32, serialized.Length)])}");
        }

        [Fact]
        public void RustBcsCompatibility_TransactionStruct_ShouldSerialize()
        {
            // Arrange - Same data as Rust example
            var transaction = new Transaction
            {
                TxId = "0x1234567890abcdef",
                FromUser = 12345,
                ToUser = 67890,
                Amount = 250,
                Timestamp = 1640995200, // 2022-01-01 00:00:00 UTC
                TxType = TransactionType.Transfer
            };

            // Act
            var transactionType = BcsStruct.Create<Transaction>();
            var serialized = transactionType.Serialize(transaction);
            var deserialized = transactionType.Parse(serialized);

            // Assert
            Assert.Equal(transaction.TxId, deserialized.TxId);
            Assert.Equal(transaction.FromUser, deserialized.FromUser);
            Assert.Equal(transaction.ToUser, deserialized.ToUser);
            Assert.Equal(transaction.Amount, deserialized.Amount);
            Assert.Equal(transaction.Timestamp, deserialized.Timestamp);
            Assert.Equal(transaction.TxType, deserialized.TxType);

            Console.WriteLine($"📋 Transaction serialized:");
            Console.WriteLine($"   Size: {serialized.Length} bytes");
            Console.WriteLine($"   Hex:  {Convert.ToHexString(serialized[..Math.Min(32, serialized.Length)])}");
        }

        [Fact]
        public void RustBcsCompatibility_MarketplaceItemStruct_ShouldSerialize()
        {
            // Arrange - Same data as Rust example (nested GameAsset)
            var asset = new GameAsset
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

            var marketplaceItem = new MarketplaceItem
            {
                ItemId = "market_001",
                Seller = 12345,
                Asset = asset,
                Price = 500,
                ListedAt = 1640995200,
                IsActive = true
            };

            // Act
            var marketplaceType = BcsStruct.Create<MarketplaceItem>();
            var serialized = marketplaceType.Serialize(marketplaceItem);
            var deserialized = marketplaceType.Parse(serialized);

            // Assert
            Assert.Equal(marketplaceItem.ItemId, deserialized.ItemId);
            Assert.Equal(marketplaceItem.Seller, deserialized.Seller);
            Assert.Equal(marketplaceItem.Price, deserialized.Price);
            Assert.Equal(marketplaceItem.ListedAt, deserialized.ListedAt);
            Assert.Equal(marketplaceItem.IsActive, deserialized.IsActive);

            // Assert nested GameAsset
            Assert.Equal(asset.AssetId, deserialized.Asset.AssetId);
            Assert.Equal(asset.AssetType, deserialized.Asset.AssetType);
            Assert.Equal(asset.Rarity, deserialized.Asset.Rarity);
            Assert.Equal(asset.Level, deserialized.Asset.Level);
            Assert.Equal(2, deserialized.Asset.Attributes.Length);

            Console.WriteLine($"📋 MarketplaceItem serialized:");
            Console.WriteLine($"   Size: {serialized.Length} bytes");
            Console.WriteLine($"   Hex:  {Convert.ToHexString(serialized[..Math.Min(32, serialized.Length)])}");
        }

        [Fact]
        public void RustBcsCompatibility_SuiCompatibleData_ShouldSerialize()
        {
            // Arrange - Same data as Rust BCS compliance test
            var bcsData = new SuiCompatibleData
            {
                Owner = "0x123456789abcdef123456789abcdef123456789abcdef123456789abcdef12",
                Balance = 1000,
                Metadata = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };

            // Act
            var bcsDataType = BcsStruct.Create<SuiCompatibleData>();
            var serialized = bcsDataType.Serialize(bcsData);
            var deserialized = bcsDataType.Parse(serialized);

            // Assert
            Assert.Equal(bcsData.Owner, deserialized.Owner);
            Assert.Equal(bcsData.Balance, deserialized.Balance);
            Assert.Equal(bcsData.Metadata, deserialized.Metadata);

            // Test deterministic property (like Rust)
            var serialized2 = bcsDataType.Serialize(bcsData);
            Assert.Equal(serialized, serialized2);

            Console.WriteLine($"📋 SuiCompatibleData serialized:");
            Console.WriteLine($"   Size: {serialized.Length} bytes");
            Console.WriteLine($"   Owner: {deserialized.Owner[..10]}...");
            Console.WriteLine($"   ✅ BCS deterministic property verified");
        }

        [Fact]
        public void RustBcsCompatibility_CompleteDemo_ShouldWork()
        {
            // Arrange - Full demo like Rust main.rs
            var user = new User
            {
                Id = 12345,
                Name = "Alice",
                Email = "alice@example.com", 
                Balance = 1000,
                IsVerified = true
            };

            var asset = new GameAsset
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
                FromUser = user.Id,
                ToUser = 67890,
                Amount = 250,
                Timestamp = 1640995200,
                TxType = TransactionType.Transfer
            };

            var marketplaceItem = new MarketplaceItem
            {
                ItemId = "market_001", 
                Seller = user.Id,
                Asset = asset,
                Price = 500,
                ListedAt = 1640995200,
                IsActive = true
            };

            // Act - Serialize all structures
            var userType = BcsStruct.Create<User>();
            var assetType = BcsStruct.Create<GameAsset>();
            var transactionType = BcsStruct.Create<Transaction>();
            var marketplaceType = BcsStruct.Create<MarketplaceItem>();

            var userBytes = userType.Serialize(user);
            var assetBytes = assetType.Serialize(asset);
            var transactionBytes = transactionType.Serialize(transaction);
            var marketplaceBytes = marketplaceType.Serialize(marketplaceItem);

            // Assert - Round-trip serialization
            var deserializedUser = userType.Parse(userBytes);
            var deserializedAsset = assetType.Parse(assetBytes);
            var deserializedTransaction = transactionType.Parse(transactionBytes);
            var deserializedMarketplace = marketplaceType.Parse(marketplaceBytes);

            Assert.Equal(user.Name, deserializedUser.Name);
            Assert.Equal(asset.AssetId, deserializedAsset.AssetId);
            Assert.Equal(transaction.TxId, deserializedTransaction.TxId);
            Assert.Equal(marketplaceItem.ItemId, deserializedMarketplace.ItemId);

            // Print results like Rust demo
            Console.WriteLine("🚀 C# BCS Serialization Demo");
            Console.WriteLine($"{"=".PadRight(40, '=')}");
            Console.WriteLine();
            Console.WriteLine("📦 All structs serialized successfully:");
            Console.WriteLine($"   User: {userBytes.Length} bytes");
            Console.WriteLine($"   GameAsset: {assetBytes.Length} bytes");
            Console.WriteLine($"   Transaction: {transactionBytes.Length} bytes");
            Console.WriteLine($"   MarketplaceItem: {marketplaceBytes.Length} bytes");
            Console.WriteLine();
            Console.WriteLine("🔄 Testing serialization round-trip...");
            Console.WriteLine($"   ✅ User serialization round-trip successful");
            Console.WriteLine($"   Original: {user.Name} ({user.Email})");
            Console.WriteLine($"   Restored: {deserializedUser.Name} ({deserializedUser.Email})");
        }
    }
}
