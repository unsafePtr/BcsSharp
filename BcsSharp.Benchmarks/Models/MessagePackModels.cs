using MessagePack;

namespace BcsSharp.Benchmarks.MsgPack;

// Mirrors the BCS models field for field.
// Integer keys select MessagePack's array encoding, so no member names go on the wire, and the source generator emits a formatter for each type at compile time.
// Email is the one deliberate difference: string? is MessagePack's idiomatic optional, where BCS needs Option<string> because it has no null string.

[MessagePackObject]
public sealed class User
{
    [Key(0)] public ulong Id { get; set; }
    [Key(1)] public string Name { get; set; } = "";
    [Key(2)] public string? Email { get; set; }
    [Key(3)] public bool IsVerified { get; set; }
    [Key(4)] public UInt128 Balance { get; set; }
}

[MessagePackObject]
public sealed class GameData
{
    [Key(0)] public string PlayerId { get; set; } = "";
    [Key(1)] public Dictionary<string, long> Inventory { get; set; } = new();
    [Key(2)] public List<Achievement> Achievements { get; set; } = new();
    [Key(3)] public PlayerStats Stats { get; set; } = new();
}

[MessagePackObject]
public sealed class Achievement
{
    [Key(0)] public string Id { get; set; } = "";
    [Key(1)] public string Name { get; set; } = "";
    [Key(2)] public UInt128 UnlockedAt { get; set; }
}

[MessagePackObject]
public sealed class PlayerStats
{
    [Key(0)] public int Level { get; set; }
    [Key(1)] public int Experience { get; set; }
    [Key(2)] public Dictionary<string, short> Attributes { get; set; } = new();
}
