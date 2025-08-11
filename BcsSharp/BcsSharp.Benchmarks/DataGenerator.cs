namespace BcsSharp.Benchmarks;

/// <summary>
/// Generates test data for benchmarking
/// </summary>
public static class DataGenerator
{
    private static readonly Random _random = new(42); // Fixed seed for reproducible results

    private static readonly string[] _firstNames =
    {
        "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry",
        "Ivy", "Jack", "Kate", "Liam", "Mia", "Noah", "Olivia", "Paul"
    };

    private static readonly string[] _achievements =
    {
        "First Login", "Level Up", "Boss Defeated", "Quest Complete", "Treasure Found",
        "PvP Victory", "Guild Master", "Explorer", "Collector", "Champion"
    };

    private static readonly string[] _items =
    {
        "Sword", "Shield", "Potion", "Armor", "Bow", "Staff", "Ring", "Gem",
        "Key", "Scroll", "Crystal", "Rune", "Helmet", "Boots", "Gloves"
    };

    private static readonly string[] _attributes =
    {
        "Strength", "Dexterity", "Intelligence", "Vitality", "Luck", "Charisma",
        "Wisdom", "Constitution", "Speed", "Endurance"
    };

    public static User GenerateUser()
    {
        var name = _firstNames[_random.Next(_firstNames.Length)];
        return new User
        {
            Id = (ulong)_random.NextInt64(1, 1000000),
            Name = name,
            Email = _random.Next(0, 3) == 0 ? null : $"{name.ToLower()}{_random.Next(100, 999)}@example.com",
            IsVerified = _random.NextDouble() > 0.3,
            Balance = new UInt128((ulong)_random.NextInt64(), (ulong)_random.NextInt64())
        };
    }

    public static List<User> GenerateUsers(int count)
    {
        return Enumerable.Range(0, count).Select(_ => GenerateUser()).ToList();
    }

    public static GameData GenerateGameData()
    {
        var playerId = Guid.NewGuid().ToString("N")[..12];

        // Generate inventory
        var inventory = new Dictionary<string, long>();
        var itemCount = _random.Next(5, 15);
        for (int i = 0; i < itemCount; i++)
        {
            var item = _items[_random.Next(_items.Length)];
            if (!inventory.ContainsKey(item))
            {
                inventory[item] = _random.Next(1, 100);
            }
        }

        // Generate achievements
        var achievements = new List<Achievement>();
        var achievementCount = _random.Next(3, 8);
        var usedAchievements = new HashSet<string>();

        for (int i = 0; i < achievementCount; i++)
        {
            string achievementName;
            do
            {
                achievementName = _achievements[_random.Next(_achievements.Length)];
            } while (usedAchievements.Contains(achievementName));

            usedAchievements.Add(achievementName);
            achievements.Add(new Achievement
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Name = achievementName,
                UnlockedAt = UInt128.MaxValue / 2 + 100_000
            });
        }

        // Generate stats
        var attributes = new Dictionary<string, short>();
        var attrCount = _random.Next(4, _attributes.Length);
        for (int i = 0; i < attrCount; i++)
        {
            var attr = _attributes[_random.Next(_attributes.Length)];
            if (!attributes.ContainsKey(attr))
            {
                attributes[attr] = (short)(_random.Next(0, short.MaxValue));
            }
        }

        var stats = new PlayerStats
        {
            Level = _random.Next(1, 100),
            Experience = _random.Next(),
            Attributes = attributes
        };

        return new GameData
        {
            PlayerId = playerId,
            Inventory = inventory,
            Achievements = achievements,
            Stats = stats
        };
    }

    public static List<GameData> GenerateGameDataList(int count)
    {
        return Enumerable.Range(0, count).Select(_ => GenerateGameData()).ToList();
    }

    public static Dictionary<string, int> GenerateStringIntMap(int size)
    {
        var map = new Dictionary<string, int>();
        for (int i = 0; i < size; i++)
        {
            var key = $"key_{i:D6}_{_random.Next(1000, 9999)}";
            map[key] = _random.Next(1, 1000000);
        }
        return map;
    }
}