using System.Runtime.CompilerServices;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;
using Nethermind.Int256;

namespace BcsSharp.Tests;

/// <summary>
/// C# equivalent of the Rust BCS example structs to test serialization compatibility
/// </summary>
public class RustBcsCompatibilityTests
{
    #region Enums (C# equivalent of Rust enums)

    public enum AssetType
    {
        Weapon = 100,
        Armor = 200,
        Consumable = 300,
        Material = 1_000_000,
        Currency = 2_000_000
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

    // C# 15 union mirroring Rust's `enum Rarity` — variant index is declaration order
    // (Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4), exactly what `bcs` produces.
    public sealed record class Common;

    [BcsStruct]
    public sealed record class Uncommon
    {
        [BcsField(0)]
        public uint Value { get; set; }
    }

    [BcsStruct]
    public sealed record class Rare
    {
        [BcsField(0)]
        public List<uint> Values { get; set; } = [];
    }

    [BcsStruct]
    public sealed record class Epic
    {
        [BcsField(0)]
        public string Name { get; set; } = string.Empty;
    }

    [BcsStruct]
    public sealed record class Legendary
    {
        [BcsField(0)]
        public (string, ulong, LegendaryAsset) Data { get; set; }
    }

    public union Rarity(Common, Uncommon, Rare, Epic, Legendary);

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

    [BcsStruct]
    public sealed class LegendaryAsset
    {
        [BcsField(0)]
        public string AssetId { get; set; } = string.Empty;
        [BcsField(1)]
        public byte Level { get; set; }
    }

    [BcsStruct]
    public class GameAsset
    {
        [BcsField(0)]
        public string AssetId { get; set; } = string.Empty;
        [BcsField(1)]
        public AssetType AssetType { get; set; }
        [BcsField(2)]
        public byte Level { get; set; }
        [BcsField(3)]
        public List<Attribute> Attributes { get; set; } = [];
    }

    [BcsStruct]
    public class Transaction
    {
        [BcsField(0)]
        public string TxId { get; set; } = string.Empty;
        [BcsField(1)]
        public ulong FromUser { get; set; }
        [BcsField(2)]
        public ulong ToUser { get; set; }
        [BcsField(3)]
        public ulong Amount { get; set; }
        [BcsField(4)]
        public ulong Timestamp { get; set; }
        [BcsField(5)]
        public TransactionType TxType { get; set; }
    }

    [BcsStruct]
    public class MarketplaceItem
    {
        [BcsField(0)]
        public string ItemId { get; set; } = string.Empty;
        [BcsField(1)]
        public ulong Seller { get; set; }
        [BcsField(2)]
        public GameAsset Asset { get; set; } = new();
        [BcsField(3)]
        public ulong Price { get; set; }
        [BcsField(4)]
        public ulong ListedAt { get; set; }
        [BcsField(5)]
        public bool IsActive { get; set; }
    }

    [BcsStruct]
    public class TupleExamples
    {
        [BcsField(0)]
        public (string, uint) SimplePair { get; set; }
        [BcsField(1)]
        public (ulong, bool, string) Triple { get; set; }
        [BcsField(2)]
        public (string, GameAsset) NestedTuple { get; set; }
        [BcsField(3)]
        public (string, List<uint>) TupleWithArray { get; set; }
        [BcsField(4)]
        public (User, Transaction, bool) ComplexTuple { get; set; }
    }

    [BcsStruct]
    public class MapExamples
    {
        [BcsField(0)]
        public Dictionary<string, uint> StringToNumber { get; set; } = new();
        [BcsField(1)]
        public Dictionary<uint, string> NumberToString { get; set; } = new();
        [BcsField(2)]
        public Dictionary<string, Attribute> UserAttributes { get; set; } = new();
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
        public Option<Address> Address { get; set; } = None.Instance;
    }

    [BcsStruct]
    public class Attribute
    {
        [BcsField(0)]
        public string Name { get; set; } = string.Empty;
        [BcsField(1)]
        public uint Value { get; set; }
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
            Address = new Address  // Some(Address) in Rust - OneOf implicit conversion
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

        // Analyze field by field to find where they differ
        AnalyzeUserStructBytes(serialized, expectedBytes);

        Assert.Equal(expectedBytes, serialized);
    }

    [Fact]
    public void RustBcsCompatibility_GameAsset_ByteComparison()
    {
        // Arrange - Same data as Rust main.rs (lines 54-68)
        var asset = new GameAsset
        {
            AssetId = "sword_001",
            AssetType = AssetType.Material, // AssetType::Weapon = 0
            Level = 15,
            Attributes = [

                new Attribute { Name = "damage", Value = 85 },
                new Attribute { Name = "speed", Value = 12 }
            ]
        };

        // Act - Serialize the asset
        var serialized = BcsSerializer.Serialize(asset);

        // Debug output
        Console.WriteLine($"C# GameAsset serialized as: {Convert.ToHexString(serialized)}");
        Console.WriteLine($"Length: {serialized.Length} bytes");

        // Assert - Read expected bytes from asset.bcs file and compare
        var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "asset.bcs");
        var expectedBytes = File.ReadAllBytes(rustBcsPath);

        Console.WriteLine($"Rust GameAsset serialized as: {Convert.ToHexString(expectedBytes)}");
        Console.WriteLine($"Length: {expectedBytes.Length} bytes");

        Assert.Equal(expectedBytes, serialized);
    }

    [Fact]
    public void RustBcsCompatibility_Transaction_ByteComparison()
    {
        // Arrange - Same data as Rust main.rs (lines 71-78)
        var transaction = new Transaction
        {
            TxId = "0x1234567890abcdef",
            FromUser = 12345, // user.id
            ToUser = 67890,
            Amount = 250,
            Timestamp = 1640995200, // 2022-01-01 00:00:00 UTC
            TxType = TransactionType.Transfer // TransactionType::Transfer = 0
        };

        // Act - Serialize the transaction
        var serialized = BcsSerializer.Serialize(transaction);

        // Debug output
        Console.WriteLine($"C# Transaction serialized as: {Convert.ToHexString(serialized)}");
        Console.WriteLine($"Length: {serialized.Length} bytes");

        // Assert - Read expected bytes from transaction.bcs file and compare
        var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "transaction.bcs");
        var expectedBytes = File.ReadAllBytes(rustBcsPath);

        Console.WriteLine($"Rust Transaction serialized as: {Convert.ToHexString(expectedBytes)}");
        Console.WriteLine($"Length: {expectedBytes.Length} bytes");

        Assert.Equal(expectedBytes, serialized);
    }

    [Fact]
    public void RustBcsCompatibility_MarketplaceItem_ByteComparison()
    {
        // Arrange - Same data as Rust main.rs (lines 80-87)
        var asset = new GameAsset
        {
            AssetId = "sword_001",
            AssetType = AssetType.Material,
            Level = 15,
            Attributes = [
                new Attribute { Name = "damage", Value = 85 },
                new Attribute { Name = "speed", Value = 12 }
            ]
        };

        var marketplaceItem = new MarketplaceItem
        {
            ItemId = "market_001",
            Seller = 12345, // user.id
            Asset = asset,
            Price = 500,
            ListedAt = 1640995200,
            IsActive = true
        };

        // Act - Serialize the marketplace item
        var serialized = BcsSerializer.Serialize(marketplaceItem);

        // Debug output
        Console.WriteLine($"C# MarketplaceItem serialized as: {Convert.ToHexString(serialized)}");
        Console.WriteLine($"Length: {serialized.Length} bytes");

        // Assert - Read expected bytes from marketplace.bcs file and compare
        var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "marketplace.bcs");
        var expectedBytes = File.ReadAllBytes(rustBcsPath);

        Console.WriteLine($"Rust MarketplaceItem serialized as: {Convert.ToHexString(expectedBytes)}");
        Console.WriteLine($"Length: {expectedBytes.Length} bytes");

        Assert.Equal(expectedBytes, serialized);
    }

    [Fact]
    public void RustBcsCompatibility_TupleExamples_ByteComparison()
    {
        // Arrange - Same data as Rust main.rs (lines 90-96)
        var user = new User
        {
            Id = 12345,
            Name = "Alice",
            Email = null,
            Balance = UInt256.MaxValue,
            IsVerified = true,
            Address = new Address
            {
                Street = "",
                City = "Plovdiv",
                State = null,
                Zip = "12345"
            }
        };

        var asset = new GameAsset
        {
            AssetId = "sword_001",
            AssetType = AssetType.Material,
            Level = 15,
            Attributes = [
                new Attribute { Name = "damage", Value = 85 },
                new Attribute { Name = "speed", Value = 12 }
            ]
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
            SimplePair = ("hello", 42u),
            Triple = (123ul, true, "world"),
            NestedTuple = ("asset_ref", asset),
            TupleWithArray = ("numbers", new List<uint> { 1, 2, 3, 4, 5 }),
            ComplexTuple = (user, transaction, false)
        };

        // Act - Serialize the tuple examples
        var serialized = BcsSerializer.Serialize(tupleExamples);

        // Debug output
        Console.WriteLine($"C# TupleExamples serialized as: {Convert.ToHexString(serialized)}");
        Console.WriteLine($"Length: {serialized.Length} bytes");

        // Assert - Read expected bytes from tuples.bcs file and compare
        var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "tuples.bcs");
        var expectedBytes = File.ReadAllBytes(rustBcsPath);

        Console.WriteLine($"Rust TupleExamples serialized as: {Convert.ToHexString(expectedBytes)}");
        Console.WriteLine($"Length: {expectedBytes.Length} bytes");

        Assert.Equal(expectedBytes, serialized);
    }

    [Fact]
    public void RustBcsCompatibility_MapExamples_ByteComparison()
    {
        // Arrange - Same data as Rust MapExamples
        var stringToNumber = new Dictionary<string, uint>
        {
            ["health"] = 100,
            ["damage"] = 85,
            ["speed"] = 12
        };

        var numberToString = new Dictionary<uint, string>
        {
            [1] = "common",
            [2] = "rare",
            [3] = "epic"
        };

        var userAttributes = new Dictionary<string, Attribute>
        {
            ["power"] = new Attribute { Name = "power", Value = 150 },
            ["defense"] = new Attribute { Name = "defense", Value = 75 }
        };

        var mapExamples = new MapExamples
        {
            StringToNumber = stringToNumber,
            NumberToString = numberToString,
            UserAttributes = userAttributes
        };

        // Act - Serialize the map examples
        var serialized = BcsSerializer.Serialize(mapExamples);

        // Debug output
        Console.WriteLine($"C# MapExamples serialized as: {Convert.ToHexString(serialized)}");
        Console.WriteLine($"Length: {serialized.Length} bytes");

        // Assert - Read expected bytes from maps.bcs file and compare
        var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "maps.bcs");
        var expectedBytes = File.ReadAllBytes(rustBcsPath);

        Console.WriteLine($"Rust MapExamples serialized as: {Convert.ToHexString(expectedBytes)}");
        Console.WriteLine($"Length: {expectedBytes.Length} bytes");

        Assert.Equal(expectedBytes, serialized);
    }

    [Fact]
    public void RustBcsCompatibility_LargeStringMap_ByteComparison()
    {
        // Arrange - Same data as Rust large_string_map (15 key-value pairs)
        var largeStringMap = new Dictionary<string, uint>
        {
            ["zebra"] = 1000,
            ["alpha"] = 2000,
            ["beta"] = 3000,
            ["gamma"] = 4000,
            ["delta"] = 5000,
            ["epsilon"] = 6000,
            ["zeta"] = 7000,
            ["eta"] = 8000,
            ["theta"] = 9000,
            ["iota"] = 10000,
            ["kappa"] = 11000,
            ["lambda"] = 12000,
            ["mu"] = 13000,
            ["nu"] = 14000,
            ["xi"] = 15000
        };

        // Act - Serialize the large string map
        var serialized = BcsSerializer.Serialize(largeStringMap);

        // Debug output
        Console.WriteLine($"C# LargeStringMap serialized as: {Convert.ToHexString(serialized)}");
        Console.WriteLine($"Length: {serialized.Length} bytes");

        // Assert - Read expected bytes from large_string_map.bcs file and compare
        var rustBcsPath = Path.Combine("..", "..", "..", "..", "..", "rust-sui-bcs-test", "large_string_map.bcs");
        var expectedBytes = File.ReadAllBytes(rustBcsPath);

        Console.WriteLine($"Rust LargeStringMap serialized as: {Convert.ToHexString(expectedBytes)}");
        Console.WriteLine($"Length: {expectedBytes.Length} bytes");

        // Analyze key ordering if there's a mismatch
        if (!serialized.SequenceEqual(expectedBytes))
        {
            Console.WriteLine("\n🔍 Analyzing key ordering...");
            AnalyzeLargeStringMapBytes(serialized, expectedBytes);
        }

        Assert.Equal(expectedBytes, serialized);
    }

    private static void AnalyzeLargeStringMapBytes(byte[] csharp, byte[] rust)
    {
        Console.WriteLine("\n=== Large String Map Key Order Analysis ===");

        var csharpReader = new BcsReader(csharp);
        var rustReader = new BcsReader(rust);

        try
        {
            // Read count
            var csharpCount = csharpReader.ReadULEB32();
            var rustCount = rustReader.ReadULEB32();
            Console.WriteLine($"Count: C#={csharpCount}, Rust={rustCount} {(csharpCount == rustCount ? "✅" : "❌")}");

            Console.WriteLine("\nKey ordering comparison:");

            for (uint i = 0; i < Math.Min(csharpCount, rustCount); i++)
            {
                var csharpKey = csharpReader.ReadString();
                var csharpValue = csharpReader.Read32();

                var rustKey = rustReader.ReadString();
                var rustValue = rustReader.Read32();

                var match = csharpKey == rustKey && csharpValue == rustValue;
                Console.WriteLine($"  {i + 1,2}: C#='{csharpKey}'→{csharpValue}, Rust='{rustKey}'→{rustValue} {(match ? "✅" : "❌")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing: {ex.Message}");
        }
    }

    private static void AnalyzeUserStructBytes(byte[] csharp, byte[] rust)
    {
        Console.WriteLine("\n=== Field-by-field analysis ===");

        var csharpReader = new BcsReader(csharp);
        var rustReader = new BcsReader(rust);

        try
        {
            // Field 0: Id (u64)
            var csharpId = csharpReader.Read64();
            var rustId = rustReader.Read64();
            Console.WriteLine($"Id: C#={csharpId}, Rust={rustId} {(csharpId == rustId ? "✅" : "❌")}");

            // Field 1: Name (String)
            var csharpName = csharpReader.ReadString();
            var rustName = rustReader.ReadString();
            Console.WriteLine($"Name: C#='{csharpName}', Rust='{rustName}' {(csharpName == rustName ? "✅" : "❌")}");

            // Field 2: Email (Option<String>)
            var csharpEmailOption = csharpReader.Read8();
            var rustEmailOption = rustReader.Read8();
            Console.WriteLine($"Email Option: C#={csharpEmailOption}, Rust={rustEmailOption} {(csharpEmailOption == rustEmailOption ? "✅" : "❌")}");

            if (csharpEmailOption == 1)
            {
                var csharpEmail = csharpReader.ReadString();
                Console.WriteLine($"Email Value: C#='{csharpEmail}'");
            }
            if (rustEmailOption == 1)
            {
                var rustEmail = rustReader.ReadString();
                Console.WriteLine($"Email Value: Rust='{rustEmail}'");
            }

            // Field 3: Balance - This is where the difference likely is
            Console.WriteLine($"\nPosition before Balance: C#={csharpReader.Position}, Rust={rustReader.Position}");

            // Both now serialize Balance as 32-byte binary
            if (rustReader.Position < rust.Length && csharpReader.Position < csharp.Length)
            {
                var rustBalance = rustReader.Read256();
                var csharpBalance = csharpReader.Read256();
                Console.WriteLine($"Balance: C#={csharpBalance}, Rust={rustBalance} {(csharpBalance == rustBalance ? "✅" : "❌")}");
            }

            // Field 4: IsVerified (bool) 
            Console.WriteLine($"\nPosition before IsVerified: C#={csharpReader.Position}, Rust={rustReader.Position}");
            if (csharpReader.Position < csharp.Length && rustReader.Position < rust.Length)
            {
                var csharpVerified = csharpReader.ReadBool();
                var rustVerified = rustReader.ReadBool();
                Console.WriteLine($"IsVerified: C#={csharpVerified}, Rust={rustVerified} {(csharpVerified == rustVerified ? "✅" : "❌")}");
            }

            // Field 5: Address (Option<Address>)
            Console.WriteLine($"\nPosition before Address: C#={csharpReader.Position}, Rust={rustReader.Position}");
            if (csharpReader.Position < csharp.Length && rustReader.Position < rust.Length)
            {
                var csharpAddressOption = csharpReader.Read8();
                var rustAddressOption = rustReader.Read8();
                Console.WriteLine($"Address Option: C#={csharpAddressOption}, Rust={rustAddressOption} {(csharpAddressOption == rustAddressOption ? "✅" : "❌")}");

                if (csharpAddressOption == 1 && rustAddressOption == 1)
                {
                    var csharpStreet = csharpReader.ReadString();
                    var rustStreet = rustReader.ReadString();
                    Console.WriteLine($"Street: C#='{csharpStreet}', Rust='{rustStreet}' {(csharpStreet == rustStreet ? "✅" : "❌")}");

                    var csharpCity = csharpReader.ReadString();
                    var rustCity = rustReader.ReadString();
                    Console.WriteLine($"City: C#='{csharpCity}', Rust='{rustCity}' {(csharpCity == rustCity ? "✅" : "❌")}");

                    var csharpStateOption = csharpReader.Read8();
                    var rustStateOption = rustReader.Read8();
                    Console.WriteLine($"State Option: C#={csharpStateOption}, Rust={rustStateOption} {(csharpStateOption == rustStateOption ? "✅" : "❌")}");

                    var csharpZip = csharpReader.ReadString();
                    var rustZip = rustReader.ReadString();
                    Console.WriteLine($"Zip: C#='{csharpZip}', Rust='{rustZip}' {(csharpZip == rustZip ? "✅" : "❌")}");
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing: {ex.Message}");
            Console.WriteLine($"C# position: {csharpReader.Position}, Rust position: {rustReader.Position}");

            // Show bytes around position 15
            Console.WriteLine($"\nBytes around position 15:");
            Console.WriteLine($"C#:   {Convert.ToHexString(csharp.Skip(10).Take(20).ToArray())}");
            Console.WriteLine($"Rust: {Convert.ToHexString(rust.Skip(10).Take(20).ToArray())}");
        }
    }

    // --- Rarity union: byte-for-byte Rust BCS compatibility tests ---------------------
    //
    // Each variant's expected bytes are hand-computed from the BCS spec
    // (ULEB variant index + variant payload). Rust's `bcs` crate produces the same
    // bytes for `Rarity::<variant>`, so these vectors double as cross-implementation
    // proofs that our C# 15 `union` formatter matches Rust's tagged-enum encoding.

    [Fact]
    public void Rarity_Common_Matches_RustBcsWire()
    {
        Rarity rarity = new Common();
        var bytes = BcsSerializer.Serialize(rarity);

        // variant 0, no payload.
        Assert.Equal(new byte[] { 0x00 }, bytes);

        var back = BcsSerializer.Deserialize<Rarity>(bytes);
        Assert.IsType<Common>(((IUnion)back).Value);
    }

    [Fact]
    public void Rarity_Uncommon_Matches_RustBcsWire()
    {
        Rarity rarity = new Uncommon { Value = 42 };
        var bytes = BcsSerializer.Serialize(rarity);

        // variant 1, u32(42) little-endian.
        Assert.Equal(new byte[] { 0x01, 0x2A, 0x00, 0x00, 0x00 }, bytes);

        var back = BcsSerializer.Deserialize<Rarity>(bytes);
        var payload = Assert.IsType<Uncommon>(((IUnion)back).Value);
        Assert.Equal(42u, payload.Value);
    }

    [Fact]
    public void Rarity_Rare_Matches_RustBcsWire()
    {
        Rarity rarity = new Rare { Values = [15] };
        var bytes = BcsSerializer.Serialize(rarity);

        // variant 2, ULEB len=1, u32(15) little-endian.
        Assert.Equal(new byte[] { 0x02, 0x01, 0x0F, 0x00, 0x00, 0x00 }, bytes);

        var back = BcsSerializer.Deserialize<Rarity>(bytes);
        var payload = Assert.IsType<Rare>(((IUnion)back).Value);
        Assert.Equal(new List<uint> { 15 }, payload.Values);
    }

    [Fact]
    public void Rarity_Epic_Matches_RustBcsWire()
    {
        Rarity rarity = new Epic { Name = "Sword" };
        var bytes = BcsSerializer.Serialize(rarity);

        // variant 3, ULEB len=5, utf8 "Sword".
        Assert.Equal(new byte[] { 0x03, 0x05, (byte)'S', (byte)'w', (byte)'o', (byte)'r', (byte)'d' }, bytes);

        var back = BcsSerializer.Deserialize<Rarity>(bytes);
        var payload = Assert.IsType<Epic>(((IUnion)back).Value);
        Assert.Equal("Sword", payload.Name);
    }

    [Fact]
    public void Rarity_Legendary_Matches_RustBcsWire()
    {
        var asset = new LegendaryAsset { AssetId = "sword_001", Level = 15 };
        Rarity rarity = new Legendary { Data = ("Hero", 1000UL, asset) };

        var bytes = BcsSerializer.Serialize(rarity);

        // variant 4, tuple (String "Hero", u64 1000, LegendaryAsset).
        // - 04                            variant index
        // - 04 48 65 72 6f                ULEB(4) + "Hero"
        // - e8 03 00 00 00 00 00 00       u64(1000) LE
        // - 09 73 77 6f 72 64 5f 30 30 31 ULEB(9) + "sword_001"
        // - 0f                            byte(15)
        var expected = new byte[]
        {
            0x04,
            0x04, 0x48, 0x65, 0x72, 0x6F,
            0xE8, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x09, 0x73, 0x77, 0x6F, 0x72, 0x64, 0x5F, 0x30, 0x30, 0x31,
            0x0F,
        };
        Assert.Equal(expected, bytes);

        var back = BcsSerializer.Deserialize<Rarity>(bytes);
        var payload = Assert.IsType<Legendary>(((IUnion)back).Value);
        Assert.Equal("Hero", payload.Data.Item1);
        Assert.Equal(1000UL, payload.Data.Item2);
        Assert.Equal("sword_001", payload.Data.Item3.AssetId);
        Assert.Equal((byte)15, payload.Data.Item3.Level);
    }
}
