//using BcsSharp.Core;
//using BcsSharp.Core.Attributes;
//using BcsSharp.Core.Resolvers;
//using Nethermind.Int256;
//using OneOf;
//using OneOf.Types;

//namespace BcsSharp.Tests
//{
//    /// <summary>
//    /// Tests that verify Rust BCS compatibility using ONLY the SourceGeneratedResolver.
//    /// These tests are identical to RustBcsCompatibilityTests but use only source-generated formatters.
//    /// </summary>
//    public class SourceGeneratedRustBcsCompatibilityTests
//    {
//        #region Enums (C# equivalent of Rust enums)

//        public enum AssetType
//        {
//            Weapon = 100,
//            Armor = 200,
//            Consumable = 300,
//            Material = 1_000_000,
//            Currency = 2_000_000
//        }

//        // Simple C-style enum for basic rarity (kept for backward compatibility)
//        public enum SimpleRarity : byte
//        {
//            Common = 0,
//            Uncommon = 1,
//            Rare = 2,
//            Epic = 3,
//            Legendary = 4
//        }

//        [BcsEnum]
//        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "<Pending>")]
//        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
//        public interface Rarity
//        {
//            // Marker interface for BCS enum variants
//        }

//        [BcsEnumVariant(0)]
//        public class Common : Rarity
//        {

//        }

//        [BcsEnumVariant(1)]
//        public class Uncommon : Rarity
//        {
//            [BcsEnumData]
//            public uint Value { get; set; }
//        }

//        [BcsEnumVariant(2)]
//        public class Rare : Rarity
//        {
//            [BcsEnumData]
//            public List<uint> Values { get; set; } = [];
//        }

//        [BcsEnumVariant(3)]
//        public class Epic : Rarity
//        {
//            [BcsEnumData]
//            public string Name { get; set; } = string.Empty;
//        }

//        [BcsEnumVariant(4)]
//        public class Legendary : Rarity
//        {
//            [BcsEnumData]
//            public (string, ulong, LegendaryAsset) Data { get; set; }
//        }

//        public enum TransactionType : byte
//        {
//            Transfer = 0,
//            Purchase = 1,
//            Sale = 2,
//            Mint = 3,
//            Burn = 4
//        }

//        #endregion

//        [BcsStruct]
//        public class Address
//        {
//            [BcsField(0)]
//            public string Street { get; set; } = string.Empty;
//            [BcsField(1)]
//            public string City { get; set; } = string.Empty;
//            [BcsField(2)]
//            public string? State { get; set; }
//            [BcsField(3)]
//            public string Zip { get; set; } = string.Empty;
//        }

//        public class LegendaryAsset
//        {
//            public string AssetId { get; set; } = string.Empty;
//            public byte Level { get; set; }
//        }

//        [BcsStruct]
//        public class GameAsset
//        {
//            [BcsField(0)]
//            public string AssetId { get; set; } = string.Empty;
//            [BcsField(1)]
//            public AssetType AssetType { get; set; }
//            [BcsField(2)]
//            public byte Level { get; set; }
//            [BcsField(3)]
//            public List<Attribute> Attributes { get; set; } = [];
//        }

//        [BcsStruct]
//        public class Transaction
//        {
//            [BcsField(0)]
//            public string TxId { get; set; } = string.Empty;
//            [BcsField(1)]
//            public ulong FromUser { get; set; }
//            [BcsField(2)]
//            public ulong ToUser { get; set; }
//            [BcsField(3)]
//            public ulong Amount { get; set; }
//            [BcsField(4)]
//            public ulong Timestamp { get; set; }
//            [BcsField(5)]
//            public TransactionType TxType { get; set; }
//        }

//        [BcsStruct]
//        public class MarketplaceItem
//        {
//            [BcsField(0)]
//            public string ItemId { get; set; } = string.Empty;
//            [BcsField(1)]
//            public ulong Seller { get; set; }
//            [BcsField(2)]
//            public GameAsset Asset { get; set; } = new();
//            [BcsField(3)]
//            public ulong Price { get; set; }
//            [BcsField(4)]
//            public ulong ListedAt { get; set; }
//            [BcsField(5)]
//            public bool IsActive { get; set; }
//        }

//        [BcsStruct]
//        public class TupleExamples
//        {
//            [BcsField(0)]
//            public (string, uint) SimplePair { get; set; }
//            [BcsField(1)]
//            public (ulong, bool, string) Triple { get; set; }
//            [BcsField(2)]
//            public (string, GameAsset) NestedTuple { get; set; }
//            [BcsField(3)]
//            public (string, List<uint>) TupleWithArray { get; set; }
//            [BcsField(4)]
//            public (User, Transaction, bool) ComplexTuple { get; set; }
//        }

//        [BcsStruct]
//        public class MapExamples
//        {
//            [BcsField(0)]
//            public Dictionary<string, uint> StringToNumber { get; set; } = new();
//            [BcsField(1)]
//            public Dictionary<uint, string> NumberToString { get; set; } = new();
//            [BcsField(2)]
//            public Dictionary<string, Attribute> UserAttributes { get; set; } = new();
//        }

//        #region Structs (C# equivalent of Rust structs)

//        [BcsStruct]
//        public class User
//        {
//            [BcsField(0)]
//            public ulong Id { get; set; }
//            [BcsField(1)]
//            public string Name { get; set; } = string.Empty;
//            [BcsField(2)]
//            public string? Email { get; set; }
//            [BcsField(3)]
//            public UInt256 Balance { get; set; }  // U256 in Rust
//            [BcsField(4)]
//            public bool IsVerified { get; set; }
//            [BcsField(5)]
//            public OneOf<None, Address> Address { get; set; }
//        }

//        [BcsStruct]
//        public class Attribute
//        {
//            [BcsField(0)]
//            public string Name { get; set; } = string.Empty;
//            [BcsField(1)]
//            public uint Value { get; set; }
//        }

//        #endregion

//        // Helper method to serialize with SourceGeneratedResolver prioritized
//        private static byte[] SerializeWithSourceGenerator<T>(T value)
//        {
//            return BcsSerializer.Serialize(value, SourceGeneratedFormatterResolver.Instance);
//        }

//        [Fact]
//        public void SourceGenerated_BcsEmptyString_SerializationTest()
//        {
//            // Test how source-generated BCS serializes empty strings
//            var emptyString = "";
//            var serializedEmpty = SerializeWithSourceGenerator(emptyString);

//            // Empty string should serialize as: [0x00] (length 0)
//            var expected = new byte[] { 0x00 };

//            Console.WriteLine($"SourceGen Empty string serialized as: {Convert.ToHexString(serializedEmpty)}");
//            Console.WriteLine($"Expected: {Convert.ToHexString(expected)}");

//            Assert.Equal(expected, serializedEmpty);
//        }

//        [Fact]
//        public void SourceGenerated_BcsString_SerializationTest()
//        {
//            // Test various string serializations with source generator
//            var testStrings = new[]
//            {
//                "",
//                "A",
//                "Alice",
//                "Plovdiv",
//                "12345",
//                "test"
//            };

//            foreach (var str in testStrings)
//            {
//                var serialized = SerializeWithSourceGenerator(str);
//                Console.WriteLine($"SourceGen String '{str}' serialized as: {Convert.ToHexString(serialized)} (length: {serialized.Length} bytes)");
//            }

//            // Specific test for "test" string to compare with Rust
//            var testStr = "test";
//            var testBytes = SerializeWithSourceGenerator(testStr);
//            var expectedRust = "0474657374"; // From Rust output
//            var actualCSharp = Convert.ToHexString(testBytes);

//            Console.WriteLine($"Rust 'test': {expectedRust}");
//            Console.WriteLine($"SourceGen 'test': {actualCSharp}");

//            Assert.Equal(expectedRust, actualCSharp);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_UserStruct_ByteComparison()
//        {
//            // Arrange - Same data as Rust main.rs (lines 14-26)
//            var user = new User
//            {
//                Id = 12345,
//                Name = "Alice",
//                Email = null, // Option::None in Rust
//                Balance = UInt256.MaxValue, // U256::max_value() in Rust
//                IsVerified = true,
//                Address = new Address  // Some(Address) in Rust - OneOf implicit conversion
//                {
//                    Street = "", // empty string in Rust
//                    City = "Plovdiv",
//                    State = null, // Option::None in Rust
//                    Zip = "12345"
//                }
//            };

//            // Act - Serialize the user using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(user);

//            // Debug: Print serialized bytes
//            Console.WriteLine($"SourceGen User serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from user.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "user.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust User serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            // Verify the source-generated serialization matches Rust exactly
//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_GameAsset_ByteComparison()
//        {
//            // Arrange - Same data as Rust main.rs (lines 54-68)
//            var asset = new GameAsset
//            {
//                AssetId = "sword_001",
//                AssetType = AssetType.Material, // AssetType::Material = 3rd position (index 3)
//                Level = 15,
//                Attributes = [

//                    new Attribute { Name = "damage", Value = 85 },
//                    new Attribute { Name = "speed", Value = 12 }
//                ]
//            };

//            // Act - Serialize the asset using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(asset);

//            // Debug output
//            Console.WriteLine($"SourceGen GameAsset serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from asset.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "asset.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust GameAsset serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_Transaction_ByteComparison()
//        {
//            // Arrange - Same data as Rust main.rs (lines 71-78)
//            var transaction = new Transaction
//            {
//                TxId = "0x1234567890abcdef",
//                FromUser = 12345, // user.id
//                ToUser = 67890,
//                Amount = 250,
//                Timestamp = 1640995200, // 2022-01-01 00:00:00 UTC
//                TxType = TransactionType.Transfer // TransactionType::Transfer = 0
//            };

//            // Act - Serialize the transaction using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(transaction);

//            // Debug output
//            Console.WriteLine($"SourceGen Transaction serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from transaction.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "transaction.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust Transaction serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_MarketplaceItem_ByteComparison()
//        {
//            // Arrange - Same data as Rust main.rs (lines 80-87)
//            var asset = new GameAsset
//            {
//                AssetId = "sword_001",
//                AssetType = AssetType.Material,
//                Level = 15,
//                Attributes = [
//                    new Attribute { Name = "damage", Value = 85 },
//                    new Attribute { Name = "speed", Value = 12 }
//                ]
//            };

//            var marketplaceItem = new MarketplaceItem
//            {
//                ItemId = "market_001",
//                Seller = 12345, // user.id
//                Asset = asset,
//                Price = 500,
//                ListedAt = 1640995200,
//                IsActive = true
//            };

//            // Act - Serialize the marketplace item using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(marketplaceItem);

//            // Debug output
//            Console.WriteLine($"SourceGen MarketplaceItem serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from marketplace.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "marketplace.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust MarketplaceItem serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_TupleExamples_ByteComparison()
//        {
//            // Arrange - Same data as Rust main.rs (lines 90-96)
//            var user = new User
//            {
//                Id = 12345,
//                Name = "Alice",
//                Email = null,
//                Balance = UInt256.MaxValue,
//                IsVerified = true,
//                Address = new Address
//                {
//                    Street = "",
//                    City = "Plovdiv",
//                    State = null,
//                    Zip = "12345"
//                }
//            };

//            var asset = new GameAsset
//            {
//                AssetId = "sword_001",
//                AssetType = AssetType.Material,
//                Level = 15,
//                Attributes = [
//                    new Attribute { Name = "damage", Value = 85 },
//                    new Attribute { Name = "speed", Value = 12 }
//                ]
//            };

//            var transaction = new Transaction
//            {
//                TxId = "0x1234567890abcdef",
//                FromUser = 12345,
//                ToUser = 67890,
//                Amount = 250,
//                Timestamp = 1640995200,
//                TxType = TransactionType.Transfer
//            };

//            var tupleExamples = new TupleExamples
//            {
//                SimplePair = ("hello", 42u),
//                Triple = (123ul, true, "world"),
//                NestedTuple = ("asset_ref", asset),
//                TupleWithArray = ("numbers", [1, 2, 3, 4, 5]),
//                ComplexTuple = (user, transaction, false)
//            };

//            // Act - Serialize the tuple examples using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(tupleExamples);

//            // Debug output
//            Console.WriteLine($"SourceGen TupleExamples serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from tuples.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "tuples.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust TupleExamples serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_MapExamples_ByteComparison()
//        {
//            // Arrange - Same data as Rust MapExamples
//            var stringToNumber = new Dictionary<string, uint>
//            {
//                ["health"] = 100,
//                ["damage"] = 85,
//                ["speed"] = 12
//            };

//            var numberToString = new Dictionary<uint, string>
//            {
//                [1] = "common",
//                [2] = "rare",
//                [3] = "epic"
//            };

//            var userAttributes = new Dictionary<string, Attribute>
//            {
//                ["power"] = new Attribute { Name = "power", Value = 150 },
//                ["defense"] = new Attribute { Name = "defense", Value = 75 }
//            };

//            var mapExamples = new MapExamples
//            {
//                StringToNumber = stringToNumber,
//                NumberToString = numberToString,
//                UserAttributes = userAttributes
//            };

//            // Act - Serialize the map examples using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(mapExamples);

//            // Debug output
//            Console.WriteLine($"SourceGen MapExamples serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from maps.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "maps.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust MapExamples serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_RustBcsCompatibility_LargeStringMap_ByteComparison()
//        {
//            // Arrange - Same data as Rust large_string_map (15 key-value pairs)
//            var largeStringMap = new Dictionary<string, uint>
//            {
//                ["zebra"] = 1000,
//                ["alpha"] = 2000,
//                ["beta"] = 3000,
//                ["gamma"] = 4000,
//                ["delta"] = 5000,
//                ["epsilon"] = 6000,
//                ["zeta"] = 7000,
//                ["eta"] = 8000,
//                ["theta"] = 9000,
//                ["iota"] = 10000,
//                ["kappa"] = 11000,
//                ["lambda"] = 12000,
//                ["mu"] = 13000,
//                ["nu"] = 14000,
//                ["xi"] = 15000
//            };

//            // Act - Serialize the large string map using source-generated formatter
//            var serialized = SerializeWithSourceGenerator(largeStringMap);

//            // Debug output
//            Console.WriteLine($"SourceGen LargeStringMap serialized as: {Convert.ToHexString(serialized)}");
//            Console.WriteLine($"Length: {serialized.Length} bytes");

//            // Assert - Read expected bytes from large_string_map.bcs file and compare
//            var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "large_string_map.bcs");
//            var expectedBytes = File.ReadAllBytes(rustBcsPath);

//            Console.WriteLine($"Rust LargeStringMap serialized as: {Convert.ToHexString(expectedBytes)}");
//            Console.WriteLine($"Length: {expectedBytes.Length} bytes");

//            Assert.Equal(expectedBytes, serialized);
//        }

//        [Fact]
//        public void SourceGenerated_vs_Runtime_Comparison()
//        {
//            // Test that verifies source-generated formatters produce identical results to runtime formatters

//            // Test User struct
//            var user = new User
//            {
//                Id = 12345,
//                Name = "Alice",
//                Email = null,
//                Balance = UInt256.MaxValue,
//                IsVerified = true,
//                Address = new Address
//                {
//                    Street = "",
//                    City = "Plovdiv",
//                    State = null,
//                    Zip = "12345"
//                }
//            };

//            var sourceGenBytes = SerializeWithSourceGenerator(user);
//            var runtimeBytes = BcsSerializer.Serialize(user); // Uses default composite resolver

//            Console.WriteLine($"SourceGen User: {Convert.ToHexString(sourceGenBytes)}");
//            Console.WriteLine($"Runtime   User: {Convert.ToHexString(runtimeBytes)}");

//            Assert.Equal(runtimeBytes, sourceGenBytes);

//            // Test GameAsset struct
//            var asset = new GameAsset
//            {
//                AssetId = "sword_001",
//                AssetType = AssetType.Material,
//                Level = 15,
//                Attributes = [
//                    new Attribute { Name = "damage", Value = 85 },
//                    new Attribute { Name = "speed", Value = 12 }
//                ]
//            };

//            var sourceGenAssetBytes = SerializeWithSourceGenerator(asset);
//            var runtimeAssetBytes = BcsSerializer.Serialize(asset);

//            Console.WriteLine($"SourceGen Asset: {Convert.ToHexString(sourceGenAssetBytes)}");
//            Console.WriteLine($"Runtime   Asset: {Convert.ToHexString(runtimeAssetBytes)}");

//            Assert.Equal(runtimeAssetBytes, sourceGenAssetBytes);
//        }
//    }
//}
