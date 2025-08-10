using System.IO;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using Nethermind.Int256;
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

        // Simple C-style enum for basic rarity (kept for backward compatibility)
        public enum SimpleRarity : byte
        {
            Common = 0,
            Uncommon = 1,
            Rare = 2,
            Epic = 3,
            Legendary = 4
        }

        [BcsEnum]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public interface Rarity
        {
            // Marker interface for BCS enum variants
        }

        [BcsEnumVariant(0)]
        public class Common : Rarity
        {

        }

        [BcsEnumVariant(1)]
        public class Uncommon : Rarity
        {
            [BcsEnumData]
            public uint Value { get; set; }
        }

        [BcsEnumVariant(2)]
        public class Rare : Rarity
        {
            [BcsEnumData]
            public uint[] Values { get; set; } = Array.Empty<uint>();
        }

        [BcsEnumVariant(3)]
        public class Epic : Rarity
        {
            [BcsEnumData]
            public string Name { get; set; } = string.Empty;
        }

        [BcsEnumVariant(4)]
        public class Legendary : Rarity
        {
            [BcsEnumData]
            public (string, ulong, LegendaryAsset) Data { get; set; }
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

        [BcsStruct]
        public class Address
        {
            [BcsField(0)]
            public string Street { get; set; } = string.Empty;
            [BcsField(1)]
            public string City { get; set; } = string.Empty;
            [BcsField(2)]
            public string? State { get; set; }
            [BcsField(3)]
            public string Zip { get; set; } = string.Empty;
        }

        public class LegendaryAsset
        {
            public string AssetId { get; set; } = string.Empty;
            public byte Level { get; set; }
        }

        #region Structs (C# equivalent of Rust structs)

        [BcsStruct]
        public class User
        {
            [BcsField(0)]
            public ulong Id { get; set; }
            [BcsField(1)]
            public string Name { get; set; } = string.Empty;
            [BcsField(2)]
            public string? Email { get; set; }
            [BcsField(3)]
            public UInt256 Balance { get; set; }  // U256 in Rust
            [BcsField(4)]
            public bool IsVerified { get; set; }
            [BcsField(5)]
            public Address? Address { get; set; }
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
            public byte Level { get; set; }
            public Attribute[] Attributes { get; set; } = Array.Empty<Attribute>();
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
            public byte[] Metadata { get; set; } = Array.Empty<byte>();
        }

        public class SuiObjectExample
        {
            public string Id { get; set; } = string.Empty;
            public ulong Version { get; set; }
            public string Owner { get; set; } = string.Empty;
            public byte[] Data { get; set; } = Array.Empty<byte>();
        }

        public class TupleExamples
        {
            public (string, uint) SimplePair { get; set; }
            public (ulong, bool, string) Triple { get; set; }
            public (string, GameAsset) NestedTuple { get; set; }
            public (string, uint[]) TupleWithArray { get; set; }
            public (User, Transaction, bool) ComplexTuple { get; set; }
        }

        public class GlobalStats
        {
            public ulong TotalUsers { get; set; }
            public ulong TotalTransactions { get; set; }
            public ulong TotalVolume { get; set; }
            public (Rarity, ulong)[] AssetsByRarity { get; set; } = Array.Empty<(Rarity, ulong)>();
        }

        public class GameState
        {
            public User[] Players { get; set; } = Array.Empty<User>();
            public Transaction[] ActiveTransactions { get; set; } = Array.Empty<Transaction>();
            public MarketplaceItem[] Marketplace { get; set; } = Array.Empty<MarketplaceItem>();
            public GlobalStats GlobalStats { get; set; } = new();
        }

        #endregion

        [Fact]
        public void BcsEmptyString_SerializationTest()
        {
            // Test how C# BCS serializes empty strings
            var emptyString = "";
            var serializedEmpty = BcsSerializer.Serialize(emptyString);
            
            // Empty string should serialize as: [0x00] (length 0)
            var expected = new byte[] { 0x00 };
            
            Console.WriteLine($"Empty string serialized as: {Convert.ToHexString(serializedEmpty)}");
            Console.WriteLine($"Expected: {Convert.ToHexString(expected)}");
            
            Assert.Equal(expected, serializedEmpty);
        }

        [Fact]
        public void BcsString_SerializationTest()
        {
            // Test various string serializations
            var testStrings = new[]
            {
                "",
                "A",
                "Alice",
                "Plovdiv",
                "12345",
                "test"
            };

            foreach (var str in testStrings)
            {
                var serialized = BcsSerializer.Serialize(str);
                Console.WriteLine($"String '{str}' serialized as: {Convert.ToHexString(serialized)} (length: {serialized.Length} bytes)");
            }
            
            // Specific test for "test" string to compare with Rust
            var testStr = "test";
            var testBytes = BcsSerializer.Serialize(testStr);
            var expectedRust = "0474657374"; // From Rust output
            var actualCSharp = Convert.ToHexString(testBytes);
            
            Console.WriteLine($"Rust 'test': {expectedRust}");
            Console.WriteLine($"C#   'test': {actualCSharp}");
            
            Assert.Equal(expectedRust, actualCSharp);
        }

        [Fact]
        public void RustBcsCompatibility_UserStruct_ByteComparison()
        {
            // Arrange - Same data as Rust main.rs (lines 14-26)
            var user = new User
            {
                Id = 12345,
                Name = "Alice",
                Email = null, // Option::None in Rust
                Balance = UInt256.MaxValue, // U256::max_value() in Rust
                IsVerified = true,
                Address = new Address
                {
                    Street = "", // empty string in Rust
                    City = "Plovdiv",
                    State = null, // Option::None in Rust
                    Zip = "12345"
                }
            };

            // Act - Serialize the user
            var serialized = BcsSerializer.Serialize(user);

            // Debug: Print serialized bytes
            Console.WriteLine($"C# User serialized as: {Convert.ToHexString(serialized)}");
            Console.WriteLine($"Length: {serialized.Length} bytes");

            // Assert - Read expected bytes from user.bcs file and compare
            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "user.bcs");
            var expectedBytes = File.ReadAllBytes(rustBcsPath);
            
            Console.WriteLine($"Rust User serialized as: {Convert.ToHexString(expectedBytes)}");
            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

            Assert.Equal(expectedBytes, serialized);
        }
    }
}
