using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;

namespace BcsSharp.Benchmarks.Bcs;

[BcsStruct]
public sealed class User
{
    [BcsField(0)] public ulong Id { get; set; }
    [BcsField(1)] public string Name { get; set; } = "";
    [BcsField(2)] public Option<string> Email { get; set; } = None.Instance;
    [BcsField(3)] public bool IsVerified { get; set; }
    [BcsField(4)] public UInt128 Balance { get; set; }
}

[BcsStruct]
public sealed class GameData
{
    [BcsField(0)] public string PlayerId { get; set; } = "";
    [BcsField(1)] public Dictionary<string, long> Inventory { get; set; } = new();
    [BcsField(2)] public List<Achievement> Achievements { get; set; } = new();
    [BcsField(3)] public PlayerStats Stats { get; set; } = new();
}

[BcsStruct]
public sealed class Achievement
{
    [BcsField(0)] public string Id { get; set; } = "";
    [BcsField(1)] public string Name { get; set; } = "";
    [BcsField(2)] public UInt128 UnlockedAt { get; set; }
}

[BcsStruct]
public sealed class PlayerStats
{
    [BcsField(0)] public int Level { get; set; }
    [BcsField(1)] public int Experience { get; set; }
    [BcsField(2)] public Dictionary<string, short> Attributes { get; set; } = new();
}
