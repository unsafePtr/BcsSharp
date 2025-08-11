using BcsSharp.Core.Attributes;
using MessagePack;

namespace BcsSharp.Benchmarks;

/// <summary>
/// Simple user model for benchmarking
/// </summary>
[BcsStruct]
[MessagePackObject]
public class User
{
    [BcsField(0)]
    [Key(0)]
    public ulong Id { get; set; }

    [BcsField(1)]
    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [BcsField(2)]
    [Key(2)]
    public string? Email { get; set; }

    [BcsField(3)]
    [Key(3)]
    public bool IsVerified { get; set; }

    [BcsField(4)]
    [Key(4)]
    public UInt128 Balance { get; set; }
}

/// <summary>
/// Complex nested structure for benchmarking
/// </summary>
[BcsStruct]
[MessagePackObject]
public class GameData
{
    [BcsField(0)]
    [Key(0)]
    public string PlayerId { get; set; } = string.Empty;

    [BcsField(1)]
    [Key(1)]
    public Dictionary<string, long> Inventory { get; set; } = new();

    [BcsField(2)]
    [Key(2)]
    public List<Achievement> Achievements { get; set; } = new();

    [BcsField(3)]
    [Key(3)]
    public PlayerStats Stats { get; set; } = new();
}

[BcsStruct]
[MessagePackObject]
public class Achievement
{
    [BcsField(0)]
    [Key(0)]
    public string Id { get; set; } = string.Empty;

    [BcsField(1)]
    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [BcsField(2)]
    [Key(2)]
    public UInt128 UnlockedAt { get; set; }
}

[BcsStruct]
[MessagePackObject]
public class PlayerStats
{
    [BcsField(0)]
    [Key(0)]
    public int Level { get; set; }

    [BcsField(1)]
    [Key(1)]
    public int Experience { get; set; }

    [BcsField(2)]
    [Key(2)]
    public Dictionary<string, short> Attributes { get; set; } = new();
}