using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BcsSharp.Core;
using MessagePack;
using System.Buffers;

namespace BcsSharp.Benchmarks;

/// <summary>
/// Benchmarks comparing BCS vs MessagePack performance for primitive types and arrays.
/// Uses dedicated buffer pools for optimal memory management and realistic performance measurement.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class PrimitiveBenchmarks
{
    // Test data for different string sizes
    private string _smallString = null!;   // 20 characters
    private string _mediumString = null!;  // 200 characters
    private string _largeString = null!;   // 2000 characters
    
    // Test data for primitive types
    private UInt128 _uint128Value;
    private ulong _ulongValue;
    
    // Test data for arrays (same sizes as strings)
    private ushort[] _smallUShortArray = null!;   // 20 elements
    private ushort[] _mediumUShortArray = null!;  // 200 elements
    private ushort[] _largeUShortArray = null!;   // 2000 elements
    
    private uint[] _smallUIntArray = null!;       // 20 elements
    private uint[] _mediumUIntArray = null!;      // 200 elements
    private uint[] _largeUIntArray = null!;       // 2000 elements
    
    // Buffer pools for optimal memory management
    private ArrayPool<byte> _bcsBufferPool = null!;
    private ArrayPool<byte> _msgPackBufferPool = null!;
    
    // Pre-serialized data for deserialization benchmarks
    private byte[] _bcsSmallStringBytes = null!;
    private byte[] _bcsMediumStringBytes = null!;
    private byte[] _bcsLargeStringBytes = null!;
    private byte[] _bcsUInt128Bytes = null!;
    private byte[] _bcsULongBytes = null!;
    private byte[] _bcsSmallUShortArrayBytes = null!;
    private byte[] _bcsMediumUShortArrayBytes = null!;
    private byte[] _bcsLargeUShortArrayBytes = null!;
    private byte[] _bcsSmallUIntArrayBytes = null!;
    private byte[] _bcsMediumUIntArrayBytes = null!;
    private byte[] _bcsLargeUIntArrayBytes = null!;
    
    private byte[] _msgPackSmallStringBytes = null!;
    private byte[] _msgPackMediumStringBytes = null!;
    private byte[] _msgPackLargeStringBytes = null!;
    private byte[] _msgPackUInt128Bytes = null!;
    private byte[] _msgPackULongBytes = null!;
    private byte[] _msgPackSmallUShortArrayBytes = null!;
    private byte[] _msgPackMediumUShortArrayBytes = null!;
    private byte[] _msgPackLargeUShortArrayBytes = null!;
    private byte[] _msgPackSmallUIntArrayBytes = null!;
    private byte[] _msgPackMediumUIntArrayBytes = null!;
    private byte[] _msgPackLargeUIntArrayBytes = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        // Create dedicated buffer pools
        _bcsBufferPool = ArrayPool<byte>.Create();
        _msgPackBufferPool = ArrayPool<byte>.Create();
        
        // Generate test strings of different sizes
        _smallString = new string('A', 20);
        _mediumString = new string('B', 200);
        _largeString = new string('C', 2000);
        
        // Generate primitive test values
        _uint128Value = new UInt128(0x123456789ABCDEF0, 0xFEDCBA0987654321);
        _ulongValue = 0xDEADBEEFCAFEBABE;
        
        // Generate test arrays
        _smallUShortArray = Enumerable.Range(0, 20).Select(i => (ushort)(i * 123)).ToArray();
        _mediumUShortArray = Enumerable.Range(0, 200).Select(i => (ushort)(i * 456)).ToArray();
        _largeUShortArray = Enumerable.Range(0, 2000).Select(i => (ushort)(i * 789)).ToArray();
        
        _smallUIntArray = Enumerable.Range(0, 20).Select(i => (uint)(i * 12345)).ToArray();
        _mediumUIntArray = Enumerable.Range(0, 200).Select(i => (uint)(i * 67890)).ToArray();
        _largeUIntArray = Enumerable.Range(0, 2000).Select(i => (uint)(i * 54321)).ToArray();
        
        // Pre-serialize all test data for deserialization benchmarks
        _bcsSmallStringBytes = BcsSerializer.Serialize(_smallString);
        _bcsMediumStringBytes = BcsSerializer.Serialize(_mediumString);
        _bcsLargeStringBytes = BcsSerializer.Serialize(_largeString);
        _bcsUInt128Bytes = BcsSerializer.Serialize(_uint128Value);
        _bcsULongBytes = BcsSerializer.Serialize(_ulongValue);
        _bcsSmallUShortArrayBytes = BcsSerializer.Serialize(_smallUShortArray);
        _bcsMediumUShortArrayBytes = BcsSerializer.Serialize(_mediumUShortArray);
        _bcsLargeUShortArrayBytes = BcsSerializer.Serialize(_largeUShortArray);
        _bcsSmallUIntArrayBytes = BcsSerializer.Serialize(_smallUIntArray);
        _bcsMediumUIntArrayBytes = BcsSerializer.Serialize(_mediumUIntArray);
        _bcsLargeUIntArrayBytes = BcsSerializer.Serialize(_largeUIntArray);
        
        _msgPackSmallStringBytes = MessagePackSerializer.Serialize(_smallString);
        _msgPackMediumStringBytes = MessagePackSerializer.Serialize(_mediumString);
        _msgPackLargeStringBytes = MessagePackSerializer.Serialize(_largeString);
        _msgPackUInt128Bytes = MessagePackSerializer.Serialize(_uint128Value);
        _msgPackULongBytes = MessagePackSerializer.Serialize(_ulongValue);
        _msgPackSmallUShortArrayBytes = MessagePackSerializer.Serialize(_smallUShortArray);
        _msgPackMediumUShortArrayBytes = MessagePackSerializer.Serialize(_mediumUShortArray);
        _msgPackLargeUShortArrayBytes = MessagePackSerializer.Serialize(_largeUShortArray);
        _msgPackSmallUIntArrayBytes = MessagePackSerializer.Serialize(_smallUIntArray);
        _msgPackMediumUIntArrayBytes = MessagePackSerializer.Serialize(_mediumUIntArray);
        _msgPackLargeUIntArrayBytes = MessagePackSerializer.Serialize(_largeUIntArray);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        // ArrayPool instances are managed by the runtime, no cleanup needed
    }
    
    // ========== BCS String Serialization ==========
    
    [Benchmark(Description = "BCS - Serialize String (20 chars)")]
    public byte[] BcsSerializeSmallString()
    {
        var buffer = _bcsBufferPool.Rent(1024);
        try
        {
            return BcsSerializer.Serialize(_smallString);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize String (200 chars)")]
    public byte[] BcsSerializeMediumString()
    {
        var buffer = _bcsBufferPool.Rent(1024);
        try
        {
            return BcsSerializer.Serialize(_mediumString);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize String (2000 chars)")]
    public byte[] BcsSerializeLargeString()
    {
        var buffer = _bcsBufferPool.Rent(4096);
        try
        {
            return BcsSerializer.Serialize(_largeString);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Deserialize String (20 chars)")]
    public string BcsDeserializeSmallString()
    {
        var buffer = _bcsBufferPool.Rent(1024);
        try
        {
            return BcsSerializer.Deserialize<string>(_bcsSmallStringBytes);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Deserialize String (200 chars)")]
    public string BcsDeserializeMediumString()
    {
        var buffer = _bcsBufferPool.Rent(1024);
        try
        {
            return BcsSerializer.Deserialize<string>(_bcsMediumStringBytes);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Deserialize String (2000 chars)")]
    public string BcsDeserializeLargeString()
    {
        var buffer = _bcsBufferPool.Rent(4096);
        try
        {
            return BcsSerializer.Deserialize<string>(_bcsLargeStringBytes);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    // ========== BCS Primitive Serialization ==========
    
    [Benchmark(Description = "BCS - Serialize UInt128")]
    public byte[] BcsSerializeUInt128()
    {
        var buffer = _bcsBufferPool.Rent(64);
        try
        {
            return BcsSerializer.Serialize(_uint128Value);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize ULong")]
    public byte[] BcsSerializeULong()
    {
        var buffer = _bcsBufferPool.Rent(32);
        try
        {
            return BcsSerializer.Serialize(_ulongValue);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Deserialize UInt128")]
    public UInt128 BcsDeserializeUInt128()
    {
        var buffer = _bcsBufferPool.Rent(64);
        try
        {
            return BcsSerializer.Deserialize<UInt128>(_bcsUInt128Bytes);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Deserialize ULong")]
    public ulong BcsDeserializeULong()
    {
        var buffer = _bcsBufferPool.Rent(32);
        try
        {
            return BcsSerializer.Deserialize<ulong>(_bcsULongBytes);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    // ========== BCS Array Serialization ==========
    
    [Benchmark(Description = "BCS - Serialize UShort[] (20 elements)")]
    public byte[] BcsSerializeSmallUShortArray()
    {
        var buffer = _bcsBufferPool.Rent(1024);
        try
        {
            return BcsSerializer.Serialize(_smallUShortArray);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize UShort[] (200 elements)")]
    public byte[] BcsSerializeMediumUShortArray()
    {
        var buffer = _bcsBufferPool.Rent(2048);
        try
        {
            return BcsSerializer.Serialize(_mediumUShortArray);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize UShort[] (2000 elements)")]
    public byte[] BcsSerializeLargeUShortArray()
    {
        var buffer = _bcsBufferPool.Rent(8192);
        try
        {
            return BcsSerializer.Serialize(_largeUShortArray);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize UInt[] (20 elements)")]
    public byte[] BcsSerializeSmallUIntArray()
    {
        var buffer = _bcsBufferPool.Rent(1024);
        try
        {
            return BcsSerializer.Serialize(_smallUIntArray);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize UInt[] (200 elements)")]
    public byte[] BcsSerializeMediumUIntArray()
    {
        var buffer = _bcsBufferPool.Rent(2048);
        try
        {
            return BcsSerializer.Serialize(_mediumUIntArray);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "BCS - Serialize UInt[] (2000 elements)")]
    public byte[] BcsSerializeLargeUIntArray()
    {
        var buffer = _bcsBufferPool.Rent(16384);
        try
        {
            return BcsSerializer.Serialize(_largeUIntArray);
        }
        finally
        {
            _bcsBufferPool.Return(buffer);
        }
    }
    
    // ========== MessagePack String Serialization ==========
    
    [Benchmark(Description = "MessagePack - Serialize String (20 chars)")]
    public byte[] MessagePackSerializeSmallString()
    {
        var buffer = _msgPackBufferPool.Rent(1024);
        try
        {
            return MessagePackSerializer.Serialize(_smallString);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize String (200 chars)")]
    public byte[] MessagePackSerializeMediumString()
    {
        var buffer = _msgPackBufferPool.Rent(1024);
        try
        {
            return MessagePackSerializer.Serialize(_mediumString);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize String (2000 chars)")]
    public byte[] MessagePackSerializeLargeString()
    {
        var buffer = _msgPackBufferPool.Rent(4096);
        try
        {
            return MessagePackSerializer.Serialize(_largeString);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Deserialize String (20 chars)")]
    public string MessagePackDeserializeSmallString()
    {
        var buffer = _msgPackBufferPool.Rent(1024);
        try
        {
            return MessagePackSerializer.Deserialize<string>(_msgPackSmallStringBytes);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Deserialize String (200 chars)")]
    public string MessagePackDeserializeMediumString()
    {
        var buffer = _msgPackBufferPool.Rent(1024);
        try
        {
            return MessagePackSerializer.Deserialize<string>(_msgPackMediumStringBytes);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Deserialize String (2000 chars)")]
    public string MessagePackDeserializeLargeString()
    {
        var buffer = _msgPackBufferPool.Rent(4096);
        try
        {
            return MessagePackSerializer.Deserialize<string>(_msgPackLargeStringBytes);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    // ========== MessagePack Primitive Serialization ==========
    
    [Benchmark(Description = "MessagePack - Serialize UInt128")]
    public byte[] MessagePackSerializeUInt128()
    {
        var buffer = _msgPackBufferPool.Rent(64);
        try
        {
            return MessagePackSerializer.Serialize(_uint128Value);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize ULong")]
    public byte[] MessagePackSerializeULong()
    {
        var buffer = _msgPackBufferPool.Rent(32);
        try
        {
            return MessagePackSerializer.Serialize(_ulongValue);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Deserialize UInt128")]
    public UInt128 MessagePackDeserializeUInt128()
    {
        var buffer = _msgPackBufferPool.Rent(64);
        try
        {
            return MessagePackSerializer.Deserialize<UInt128>(_msgPackUInt128Bytes);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Deserialize ULong")]
    public ulong MessagePackDeserializeULong()
    {
        var buffer = _msgPackBufferPool.Rent(32);
        try
        {
            return MessagePackSerializer.Deserialize<ulong>(_msgPackULongBytes);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    // ========== MessagePack Array Serialization ==========
    
    [Benchmark(Description = "MessagePack - Serialize UShort[] (20 elements)")]
    public byte[] MessagePackSerializeSmallUShortArray()
    {
        var buffer = _msgPackBufferPool.Rent(1024);
        try
        {
            return MessagePackSerializer.Serialize(_smallUShortArray);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize UShort[] (200 elements)")]
    public byte[] MessagePackSerializeMediumUShortArray()
    {
        var buffer = _msgPackBufferPool.Rent(2048);
        try
        {
            return MessagePackSerializer.Serialize(_mediumUShortArray);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize UShort[] (2000 elements)")]
    public byte[] MessagePackSerializeLargeUShortArray()
    {
        var buffer = _msgPackBufferPool.Rent(8192);
        try
        {
            return MessagePackSerializer.Serialize(_largeUShortArray);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize UInt[] (20 elements)")]
    public byte[] MessagePackSerializeSmallUIntArray()
    {
        var buffer = _msgPackBufferPool.Rent(1024);
        try
        {
            return MessagePackSerializer.Serialize(_smallUIntArray);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize UInt[] (200 elements)")]
    public byte[] MessagePackSerializeMediumUIntArray()
    {
        var buffer = _msgPackBufferPool.Rent(2048);
        try
        {
            return MessagePackSerializer.Serialize(_mediumUIntArray);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
    
    [Benchmark(Description = "MessagePack - Serialize UInt[] (2000 elements)")]
    public byte[] MessagePackSerializeLargeUIntArray()
    {
        var buffer = _msgPackBufferPool.Rent(16384);
        try
        {
            return MessagePackSerializer.Serialize(_largeUIntArray);
        }
        finally
        {
            _msgPackBufferPool.Return(buffer);
        }
    }
}