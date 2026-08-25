using BcsSharp.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using MessagePack;

namespace BcsSharp.Benchmarks;

public class SizeConfig : ManualConfig
{
    public SizeConfig()
    {
        AddColumn(new SizeColumn());
    }
}

/// <summary>
/// Benchmarks to compare serialized size between BCS and MessagePack
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(SizeConfig))]
public class SizeBenchmarks
{
    private User _singleUser = null!;
    private List<User> _userList = null!;
    private GameData _complexData = null!;
    private Dictionary<string, int> _largeMap = null!;

    [GlobalSetup]
    public void Setup()
    {
        _singleUser = DataGenerator.GenerateUser();
        _userList = DataGenerator.GenerateUsers(100);
        _complexData = DataGenerator.GenerateGameData();
        _largeMap = DataGenerator.GenerateStringIntMap(1000);

        // Clear any previous size cache
        SizeCache.Clear();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Print size comparison after benchmarks
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("SIZE COMPARISON SUMMARY");
        Console.WriteLine("=".PadRight(60, '='));

        PrintSizeComparison("User", _singleUser);
        PrintSizeComparison("User List (100)", _userList);
        PrintSizeComparison("GameData", _complexData);
        PrintSizeComparison("Large Map (1000)", _largeMap);
    }

    private static void PrintSizeComparison<T>(string name, T obj)
    {
        var bcsBytes = BcsSerializer.Serialize(obj);
        var msgPackBytes = MessagePackSerializer.Serialize(obj);
        var ratio = (double)bcsBytes.Length / msgPackBytes.Length;
        var diff = bcsBytes.Length - msgPackBytes.Length;

        Console.WriteLine($"{name}:");
        Console.WriteLine($"  BCS:        {bcsBytes.Length,6} bytes");
        Console.WriteLine($"  MessagePack: {msgPackBytes.Length,6} bytes");
        Console.WriteLine($"  Ratio:       {ratio,6:F2}x ({diff:+#;-#;0} bytes)");
        Console.WriteLine();
    }

    // ========== BCS Size Benchmarks ==========

    [Benchmark(Description = "BCS User")]
    public byte[] BcsUserSize()
    {
        var bytes = BcsSerializer.Serialize(_singleUser);
        SizeCache.SetSize(nameof(BcsUserSize), bytes.Length);
        return bytes;
    }

    [Benchmark(Description = "BCS UserList")]
    public byte[] BcsUserListSize()
    {
        var bytes = BcsSerializer.Serialize(_userList);
        SizeCache.SetSize(nameof(BcsUserListSize), bytes.Length);
        return bytes;
    }

    [Benchmark(Description = "BCS GameData")]
    public byte[] BcsGameDataSize()
    {
        var bytes = BcsSerializer.Serialize(_complexData);
        SizeCache.SetSize(nameof(BcsGameDataSize), bytes.Length);
        return bytes;
    }

    [Benchmark(Description = "BCS LargeMap")]
    public byte[] BcsLargeMapSize()
    {
        var bytes = BcsSerializer.Serialize(_largeMap);
        SizeCache.SetSize(nameof(BcsLargeMapSize), bytes.Length);
        return bytes;
    }

    // ========== MessagePack Size Benchmarks ==========

    [Benchmark(Description = "MessagePack User")]
    public byte[] MessagePackUserSize()
    {
        var bytes = MessagePackSerializer.Serialize(_singleUser);
        SizeCache.SetSize(nameof(MessagePackUserSize), bytes.Length);
        return bytes;
    }

    [Benchmark(Description = "MessagePack UserList")]
    public byte[] MessagePackUserListSize()
    {
        var bytes = MessagePackSerializer.Serialize(_userList);
        SizeCache.SetSize(nameof(MessagePackUserListSize), bytes.Length);
        return bytes;
    }

    [Benchmark(Description = "MessagePack GameData")]
    public byte[] MessagePackGameDataSize()
    {
        var bytes = MessagePackSerializer.Serialize(_complexData);
        SizeCache.SetSize(nameof(MessagePackGameDataSize), bytes.Length);
        return bytes;
    }

    [Benchmark(Description = "MessagePack LargeMap")]
    public byte[] MessagePackLargeMapSize()
    {
        var bytes = MessagePackSerializer.Serialize(_largeMap);
        SizeCache.SetSize(nameof(MessagePackLargeMapSize), bytes.Length);
        return bytes;
    }
}
