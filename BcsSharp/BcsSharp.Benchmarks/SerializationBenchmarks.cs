using BcsSharp.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using MessagePack;

namespace BcsSharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class SerializationBenchmarks
{
    private User _singleUser = null!;
    private List<User> _userList = null!;
    private GameData _complexData = null!;
    private List<GameData> _complexDataList = null!;
    private Dictionary<string, int> _largeMap = null!;

    private byte[] _bcsUserBytes = null!;
    private byte[] _msgPackUserBytes = null!;
    private byte[] _bcsComplexBytes = null!;
    private byte[] _msgPackComplexBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test data
        _singleUser = DataGenerator.GenerateUser();
        _userList = DataGenerator.GenerateUsers(100);
        _complexData = DataGenerator.GenerateGameData();
        _complexDataList = DataGenerator.GenerateGameDataList(50);
        _largeMap = DataGenerator.GenerateStringIntMap(1000);

        // Pre-serialize for deserialization benchmarks
        _bcsUserBytes = BcsSerializer.Serialize(_singleUser);
        _msgPackUserBytes = MessagePackSerializer.Serialize(_singleUser);
        _bcsComplexBytes = BcsSerializer.Serialize(_complexData);
        _msgPackComplexBytes = MessagePackSerializer.Serialize(_complexData);
    }

    // ========== BCS Serialization ==========

    [Benchmark(Description = "BCS - Serialize User")]
    public byte[] BcsSerializeUser()
    {
        return BcsSerializer.Serialize(_singleUser);
    }

    [Benchmark(Description = "BCS - Deserialize User")]
    public User BcsDeserializeUser()
    {
        return BcsSerializer.Deserialize<User>(_bcsUserBytes);
    }

    [Benchmark(Description = "BCS - Serialize User List")]
    public byte[] BcsSerializeUserList()
    {
        return BcsSerializer.Serialize(_userList);
    }

    [Benchmark(Description = "BCS - Serialize GameData")]
    public byte[] BcsSerializeComplex()
    {
        return BcsSerializer.Serialize(_complexData);
    }

    [Benchmark(Description = "BCS - Deserialize GameData")]
    public GameData BcsDeserializeComplex()
    {
        return BcsSerializer.Deserialize<GameData>(_bcsComplexBytes);
    }

    [Benchmark(Description = "BCS - Serialize Large Map")]
    public byte[] BcsSerializeLargeMap()
    {
        return BcsSerializer.Serialize(_largeMap);
    }

    [Benchmark(Description = "BCS - Serialize GameData List")]
    public byte[] BcsSerializeBulkComplex()
    {
        return BcsSerializer.Serialize(_complexDataList);
    }

    // ========== MessagePack Serialization ==========

    [Benchmark(Description = "MessagePack - Serialize User")]
    public byte[] MessagePackSerializeUser()
    {
        return MessagePackSerializer.Serialize(_singleUser);
    }

    [Benchmark(Description = "MessagePack - Deserialize User")]
    public User MessagePackDeserializeUser()
    {
        return MessagePackSerializer.Deserialize<User>(_msgPackUserBytes);
    }

    [Benchmark(Description = "MessagePack - Serialize User List")]
    public byte[] MessagePackSerializeUserList()
    {
        return MessagePackSerializer.Serialize(_userList);
    }

    [Benchmark(Description = "MessagePack - Serialize GameData")]
    public byte[] MessagePackSerializeComplex()
    {
        return MessagePackSerializer.Serialize(_complexData);
    }

    [Benchmark(Description = "MessagePack - Deserialize GameData")]
    public GameData MessagePackDeserializeComplex()
    {
        return MessagePackSerializer.Deserialize<GameData>(_msgPackComplexBytes);
    }

    [Benchmark(Description = "MessagePack - Serialize Large Map")]
    public byte[] MessagePackSerializeLargeMap()
    {
        return MessagePackSerializer.Serialize(_largeMap);
    }

    [Benchmark(Description = "MessagePack - Serialize GameData List")]
    public byte[] MessagePackSerializeBulkComplex()
    {
        return MessagePackSerializer.Serialize(_complexDataList);
    }
}
