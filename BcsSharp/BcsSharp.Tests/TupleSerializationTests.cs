using System;
using System.IO;
using BcsSharp.Core;
using Xunit;

namespace BcsSharp.Tests;

/// <summary>
/// Tuple serialization tests adapted to work with BcsSerializer
/// Tests C# ValueTuples which have built-in BCS serialization support
/// </summary>
public class TupleSerializationTests
{
    [Fact]
    public void TupleSerialization_SimplePair_ShouldWork()
    {
        // Arrange - Simple tuple (string, uint)
        var simplePair = ("hello", 42u);

        // Act
        var serialized = BcsSerializer.Serialize(simplePair);
        var deserialized = BcsSerializer.Deserialize<(string, uint)>(serialized);

        // Assert
        Assert.Equal(simplePair.Item1, deserialized.Item1);
        Assert.Equal(simplePair.Item2, deserialized.Item2);
    }

    [Fact]
    public void TupleSerialization_Triple_ShouldWork()
    {
        // Arrange - Triple (ulong, bool, string)
        var triple = (123ul, true, "world");

        // Act
        var serialized = BcsSerializer.Serialize(triple);
        var deserialized = BcsSerializer.Deserialize<(ulong, bool, string)>(serialized);

        // Assert
        Assert.Equal(triple.Item1, deserialized.Item1);
        Assert.Equal(triple.Item2, deserialized.Item2);
        Assert.Equal(triple.Item3, deserialized.Item3);
    }

    [Fact]
    public void TupleSerialization_WithLists_ShouldWork()
    {
        // Arrange - Tuple with array (string, uint[])
        var tupleWithArray = ("numbers", new List<uint> { 1, 2, 3, 4, 5 });

        // Act
        var serialized = BcsSerializer.Serialize(tupleWithArray);
        var deserialized = BcsSerializer.Deserialize<(string, List<uint>)>(serialized);

        // Assert
        Assert.Equal(tupleWithArray.Item1, deserialized.Item1);
        Assert.Equal(tupleWithArray.Item2, deserialized.Item2);
    }

    [Fact]
    public void TupleSerialization_Nested_ShouldWork()
    {
        // Arrange - Nested tuples
        var nestedTuple = ("outer", (42, true));

        // Act
        var serialized = BcsSerializer.Serialize(nestedTuple);
        var deserialized = BcsSerializer.Deserialize<(string, (int, bool))>(serialized);

        // Assert
        Assert.Equal(nestedTuple.Item1, deserialized.Item1);
        Assert.Equal(nestedTuple.Item2.Item1, deserialized.Item2.Item1);
        Assert.Equal(nestedTuple.Item2.Item2, deserialized.Item2.Item2);
    }

    [Fact]
    public void TupleSerialization_FourElementTuple_ShouldWork()
    {
        // Arrange - 4-element tuple (supported by the library)
        var largeTuple = ("test", 42u, true, (byte)255);

        // Act
        var serialized = BcsSerializer.Serialize(largeTuple);
        var deserialized = BcsSerializer.Deserialize<(string, uint, bool, byte)>(serialized);

        // Assert
        Assert.Equal(largeTuple.Item1, deserialized.Item1);
        Assert.Equal(largeTuple.Item2, deserialized.Item2);
        Assert.Equal(largeTuple.Item3, deserialized.Item3);
        Assert.Equal(largeTuple.Item4, deserialized.Item4);
    }

    [Fact]
    public void TupleSerialization_EmptyString_ShouldWork()
    {
        // Arrange - Test with empty string
        var tupleWithEmpty = ("", 0u);

        // Act
        var serialized = BcsSerializer.Serialize(tupleWithEmpty);
        var deserialized = BcsSerializer.Deserialize<(string, uint)>(serialized);

        // Assert
        Assert.Equal("", deserialized.Item1);
        Assert.Equal(0u, deserialized.Item2);
    }

    [Fact]
    public void TupleSerialization_DeterministicEncoding_ShouldWork()
    {
        // Arrange - Test deterministic property
        var testTuple = ("consistent", 42u, true);

        // Act - Serialize multiple times
        var serialization1 = BcsSerializer.Serialize(testTuple);
        var serialization2 = BcsSerializer.Serialize(testTuple);
        var serialization3 = BcsSerializer.Serialize(testTuple);

        // Assert - All serializations should be identical
        Assert.Equal(serialization1, serialization2);
        Assert.Equal(serialization2, serialization3);
    }

    // Comment out the old complex struct-based tests until BcsStruct is implemented

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
}
