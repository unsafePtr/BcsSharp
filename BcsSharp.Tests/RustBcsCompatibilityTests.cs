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
        public Option<string> State { get; set; } = None.Instance;
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
        public Option<string> Email { get; set; } = None.Instance;
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

    /// Mirrors Rust's `GlobalStats`. `Vec&lt;(Rarity, u64)&gt;` exercises the tagged-enum path through a list of tuples, which is the deepest nesting the fixtures cover.
    [BcsStruct]
    public class GlobalStats
    {
        [BcsField(0)]
        public ulong TotalUsers { get; set; }
        [BcsField(1)]
        public ulong TotalTransactions { get; set; }
        [BcsField(2)]
        public ulong TotalVolume { get; set; }
        [BcsField(3)]
        public List<(Rarity, ulong)> AssetsByRarity { get; set; } = [];
    }

    /// Mirrors Rust's `SuiCompatibleData`. `metadata` is `Vec&lt;u8&gt;`, which is length-prefixed — distinct from the fixed-width `[u8; 32]` byte-array path.
    [BcsStruct]
    public class SuiCompatibleData
    {
        [BcsField(0)]
        public string Owner { get; set; } = string.Empty;
        [BcsField(1)]
        public ulong Balance { get; set; }
        [BcsField(2)]
        public List<byte> Metadata { get; set; } = [];
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
