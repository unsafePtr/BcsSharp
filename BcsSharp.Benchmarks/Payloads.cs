using BcsSharp.Core.Unions;

namespace BcsSharp.Benchmarks;

/// <summary>
/// Builds every payload in both model sets from the same random draws, so the two serializers always encode identical data.
/// Each benchmark seeds its own <see cref="Random"/>, which lets <see cref="PayloadSizeColumn"/> rebuild the exact payload in the host process.
/// Strings and dictionaries are shared between the two graphs; serialization only reads them.
/// </summary>
public static class Payloads
{
    private static readonly string[] FirstNames =
    [
        "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry",
        "Ivy", "Jack", "Kate", "Liam", "Mia", "Noah", "Olivia", "Paul",
    ];

    private static readonly string[] AchievementNames =
    [
        "First Login", "Level Up", "Boss Defeated", "Quest Complete", "Treasure Found",
        "PvP Victory", "Guild Master", "Explorer", "Collector", "Champion",
    ];

    private static readonly string[] Items =
    [
        "Sword", "Shield", "Potion", "Armor", "Bow", "Staff", "Ring", "Gem",
        "Key", "Scroll", "Crystal", "Rune", "Helmet", "Boots", "Gloves",
    ];

    private static readonly string[] AttributeNames =
    [
        "Strength", "Dexterity", "Intelligence", "Vitality", "Luck", "Charisma",
        "Wisdom", "Constitution", "Speed", "Endurance",
    ];

    public static (Bcs.User Bcs, MsgPack.User MsgPack) User(Random random)
    {
        var name = FirstNames[random.Next(FirstNames.Length)];
        var id = (ulong)random.NextInt64(1, 1_000_000);
        var email = random.Next(0, 3) == 0 ? null : $"{name.ToLowerInvariant()}{random.Next(100, 999)}@example.com";
        var isVerified = random.NextDouble() > 0.3;
        var balance = new UInt128((ulong)random.NextInt64(), (ulong)random.NextInt64());

        var bcs = new Bcs.User { Id = id, Name = name, IsVerified = isVerified, Balance = balance };
        if (email is not null)
        {
            bcs.Email = email;
        }

        var msgPack = new MsgPack.User { Id = id, Name = name, Email = email, IsVerified = isVerified, Balance = balance };
        return (bcs, msgPack);
    }

    public static (List<Bcs.User> Bcs, List<MsgPack.User> MsgPack) Users(Random random, int count)
    {
        var bcs = new List<Bcs.User>(count);
        var msgPack = new List<MsgPack.User>(count);
        for (var i = 0; i < count; i++)
        {
            var (b, m) = User(random);
            bcs.Add(b);
            msgPack.Add(m);
        }

        return (bcs, msgPack);
    }

    public static (Bcs.GameData Bcs, MsgPack.GameData MsgPack) GameData(Random random)
    {
        var playerId = Hex(random, 12);

        var inventory = new Dictionary<string, long>();
        var itemCount = random.Next(5, 15);
        for (var i = 0; i < itemCount; i++)
        {
            inventory.TryAdd(Items[random.Next(Items.Length)], random.Next(1, 100));
        }

        var names = AchievementNames.ToArray();
        random.Shuffle(names);
        var achievementCount = random.Next(3, 8);
        var bcsAchievements = new List<Bcs.Achievement>(achievementCount);
        var msgPackAchievements = new List<MsgPack.Achievement>(achievementCount);
        for (var i = 0; i < achievementCount; i++)
        {
            var id = Hex(random, 8);
            var unlockedAt = UInt128.MaxValue / 2 + (ulong)random.NextInt64();
            bcsAchievements.Add(new Bcs.Achievement { Id = id, Name = names[i], UnlockedAt = unlockedAt });
            msgPackAchievements.Add(new MsgPack.Achievement { Id = id, Name = names[i], UnlockedAt = unlockedAt });
        }

        var attributes = new Dictionary<string, short>();
        var attributeCount = random.Next(4, AttributeNames.Length);
        for (var i = 0; i < attributeCount; i++)
        {
            attributes.TryAdd(AttributeNames[random.Next(AttributeNames.Length)], (short)random.Next(0, short.MaxValue));
        }

        var level = random.Next(1, 100);
        var experience = random.Next();

        var bcs = new Bcs.GameData
        {
            PlayerId = playerId,
            Inventory = inventory,
            Achievements = bcsAchievements,
            Stats = new Bcs.PlayerStats { Level = level, Experience = experience, Attributes = attributes },
        };
        var msgPack = new MsgPack.GameData
        {
            PlayerId = playerId,
            Inventory = inventory,
            Achievements = msgPackAchievements,
            Stats = new MsgPack.PlayerStats { Level = level, Experience = experience, Attributes = attributes },
        };
        return (bcs, msgPack);
    }

    public static (List<Bcs.GameData> Bcs, List<MsgPack.GameData> MsgPack) GameDataList(Random random, int count)
    {
        var bcs = new List<Bcs.GameData>(count);
        var msgPack = new List<MsgPack.GameData>(count);
        for (var i = 0; i < count; i++)
        {
            var (b, m) = GameData(random);
            bcs.Add(b);
            msgPack.Add(m);
        }

        return (bcs, msgPack);
    }

    public static Dictionary<string, int> StringIntMap(Random random, int count)
    {
        var map = new Dictionary<string, int>(count);
        for (var i = 0; i < count; i++)
        {
            map[$"key_{i:D6}_{random.Next(1000, 9999)}"] = random.Next(1, 1_000_000);
        }

        return map;
    }

    private static string Hex(Random random, int length)
    {
        Span<byte> bytes = stackalloc byte[length / 2];
        random.NextBytes(bytes);
        return Convert.ToHexStringLower(bytes);
    }
}
