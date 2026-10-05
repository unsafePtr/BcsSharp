using System.Buffers;
using BcsSharp.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using MessagePack;

namespace BcsSharp.Benchmarks;

[SimpleJob(RuntimeMoniker.Net11_0)]
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Config(typeof(PayloadSizeConfig))]
// MessagePack runs on its default options: StandardResolver already picks up the source-generated formatters, and wrapping them in a CompositeResolver adds a lookup per field.
public abstract class SerializationBenchmarks<TBcs, TMsgPack> : IPayloadSize
{
    private const string Serialize = "Serialize";
    private const string SerializeToBuffer = "SerializeToBuffer";
    private const string Deserialize = "Deserialize";

    private readonly ArrayBufferWriter<byte> _buffer = new(64 * 1024);
    private TBcs _bcs = default!;
    private TMsgPack _msgPack = default!;
    private byte[] _bcsBytes = null!;
    private byte[] _msgPackBytes = null!;

    protected abstract (TBcs Bcs, TMsgPack MsgPack) CreatePayload();

    [GlobalSetup]
    public void Setup()
    {
        (_bcs, _msgPack) = CreatePayload();
        _bcsBytes = BcsSerializer.Serialize(_bcs);
        _msgPackBytes = MessagePackSerializer.Serialize(_msgPack);
    }

    [Benchmark(Baseline = true, Description = "BCS"), BenchmarkCategory(Serialize)]
    public byte[] BcsSerialize() => BcsSerializer.Serialize(_bcs);

    [Benchmark(Description = "MessagePack"), BenchmarkCategory(Serialize)]
    public byte[] MessagePackSerialize() => MessagePackSerializer.Serialize(_msgPack);

    [Benchmark(Baseline = true, Description = "BCS"), BenchmarkCategory(SerializeToBuffer)]
    public void BcsSerializeToBuffer()
    {
        _buffer.ResetWrittenCount();
        BcsSerializer.Serialize(_buffer, _bcs);
    }

    [Benchmark(Description = "MessagePack"), BenchmarkCategory(SerializeToBuffer)]
    public void MessagePackSerializeToBuffer()
    {
        _buffer.ResetWrittenCount();
        MessagePackSerializer.Serialize(_buffer, _msgPack);
    }

    [Benchmark(Baseline = true, Description = "BCS"), BenchmarkCategory(Deserialize)]
    public TBcs BcsDeserialize() => BcsSerializer.Deserialize<TBcs>(_bcsBytes);

    [Benchmark(Description = "MessagePack"), BenchmarkCategory(Deserialize)]
    public TMsgPack MessagePackDeserialize() => MessagePackSerializer.Deserialize<TMsgPack>(_msgPackBytes);

    public int BcsSize() => BcsSerializer.Serialize(CreatePayload().Bcs).Length;

    public int MessagePackSize() => MessagePackSerializer.Serialize(CreatePayload().MsgPack).Length;
}

public class UserBenchmarks : SerializationBenchmarks<Bcs.User, MsgPack.User>
{
    protected override (Bcs.User Bcs, MsgPack.User MsgPack) CreatePayload() => Payloads.User(new Random(42));
}

public class UserListBenchmarks : SerializationBenchmarks<List<Bcs.User>, List<MsgPack.User>>
{
    protected override (List<Bcs.User> Bcs, List<MsgPack.User> MsgPack) CreatePayload() => Payloads.Users(new Random(42), 100);
}

public class GameDataBenchmarks : SerializationBenchmarks<Bcs.GameData, MsgPack.GameData>
{
    protected override (Bcs.GameData Bcs, MsgPack.GameData MsgPack) CreatePayload() => Payloads.GameData(new Random(42));
}

public class GameDataListBenchmarks : SerializationBenchmarks<List<Bcs.GameData>, List<MsgPack.GameData>>
{
    protected override (List<Bcs.GameData> Bcs, List<MsgPack.GameData> MsgPack) CreatePayload() => Payloads.GameDataList(new Random(42), 50);
}

public class StringMapBenchmarks : SerializationBenchmarks<Dictionary<string, int>, Dictionary<string, int>>
{
    protected override (Dictionary<string, int> Bcs, Dictionary<string, int> MsgPack) CreatePayload()
    {
        var map = Payloads.StringIntMap(new Random(42), 1000);
        return (map, map);
    }
}
