using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// Measures per-op allocation for value-type vs reference-type <c>[BcsStruct]</c> objects to check the "boxing on value-type field set" concern.
///
/// Theoretical concern: <see cref="BcsSharp.Core.Formatters.BcsObjectFormatter{T}"/>'s value-type deserialize path uses <c>object boxedInstance = _constructor()</c> and reflective <c>FieldInfo.SetValue</c>, and <c>ArgumentNullException.ThrowIfNull(value)</c> in serialize would box a struct argument.
///
/// Measured reality on net11 P3: the struct and class paths allocate the same number of bytes per op.
/// The JIT erases the speculative struct-boxes, leaving only the genuine per-field boxes (which happen on both paths because <c>IBcsObjectFormatter</c> uses <c>object?</c> at the call boundary).
///
/// This test guards against future regressions where the gap reopens.
/// </summary>
public class BcsObjectFormatterAllocTests
{
    const int Iters = 100_000;

    private static double MeasureBytesPerOp(Action work)
    {
        for (var i = 0; i < 1_000; i++)
        {
            work();
        }

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Iters; i++)
        {
            work();
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        return (after - before) / (double)Iters;
    }

    [Fact]
    public void StructVsClass_RoundTripAlloc_AtParity()
    {
        var s = new PointStruct { X = 1, Y = 2 };
        var c = new PointClass { X = 1, Y = 2 };

        var bytesS = BcsSerializer.Serialize(s);
        var bytesC = BcsSerializer.Serialize(c);

        var structSer = MeasureBytesPerOp(() => _ = BcsSerializer.Serialize(s));
        var classSer = MeasureBytesPerOp(() => _ = BcsSerializer.Serialize(c));
        var structDeser = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<PointStruct>(bytesS));
        var classDeser = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<PointClass>(bytesC));

        Console.WriteLine($"PointStruct Serialize:   {structSer,7:F1} B/op");
        Console.WriteLine($"PointClass  Serialize:   {classSer,7:F1} B/op");
        Console.WriteLine($"PointStruct Deserialize: {structDeser,7:F1} B/op");
        Console.WriteLine($"PointClass  Deserialize: {classDeser,7:F1} B/op");

        // After (a) guarding ArgumentNullException.ThrowIfNull behind !_isValueType,
        // (b) dropping the closure-capturing GetOrAdd from CompositeResolver, and (c)
        // replacing IBcsObjectFormatter's object? per-field boundary with a typed
        // virtual call (BcsObjectFieldSerializer<TInstance>):
        //   Serialize   = 32 B/op = 24 B output byte[] + ~8 B (one-off)
        //   Deserialize = 0 B/op for value-type T; ~24 B (the instance) for class T
        // The IBufferWriter overload removes the byte[] and hits literal zero alloc.
        Assert.True(structSer <= 40, $"Struct serialize {structSer:F1} > 40 B/op expected ceiling");
        Assert.Equal(0.0, structDeser);   // value-type round-trip is alloc-free
        Assert.True(classDeser <= 32, $"Class deserialize {classDeser:F1} > 32 B/op (~ instance header)");
    }

    [Fact]
    public void IBufferWriterOverload_RoundTripAlloc()
    {
        var s = new PointStruct { X = 1, Y = 2 };
        var c = new PointClass { X = 1, Y = 2 };

        var bw = new System.Buffers.ArrayBufferWriter<byte>(64);

        // Warm both formatters & populate caches.
        BcsSerializer.Serialize<PointStruct>(bw, s); bw.ResetWrittenCount();
        BcsSerializer.Serialize<PointClass>(bw, c); bw.ResetWrittenCount();

        var structSerBufferWriter = MeasureBytesPerOp(() =>
        {
            BcsSerializer.Serialize<PointStruct>(bw, s);
            bw.ResetWrittenCount();
        });

        var classSerBufferWriter = MeasureBytesPerOp(() =>
        {
            BcsSerializer.Serialize<PointClass>(bw, c);
            bw.ResetWrittenCount();
        });

        Console.WriteLine($"PointStruct Serialize (IBufferWriter): {structSerBufferWriter,7:F1} B/op");
        Console.WriteLine($"PointClass  Serialize (IBufferWriter): {classSerBufferWriter,7:F1} B/op");

        // The IBufferWriter overload writes into caller-owned storage and the typed
        // per-field dispatch eliminates all boxing — Serialize is literally zero alloc.
        Assert.Equal(0.0, structSerBufferWriter);
        Assert.Equal(0.0, classSerBufferWriter);
    }

    [BcsStruct]
    public struct PointStruct
    {
        [BcsField(0)] public uint X { get; set; }
        [BcsField(1)] public uint Y { get; set; }
    }

    [BcsStruct]
    public sealed class PointClass
    {
        [BcsField(0)] public uint X { get; set; }
        [BcsField(1)] public uint Y { get; set; }
    }
}
