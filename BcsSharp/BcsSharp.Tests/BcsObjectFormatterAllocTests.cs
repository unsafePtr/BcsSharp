using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// Measures per-op allocation for value-type vs reference-type <c>[BcsStruct]</c>
/// objects to check the "boxing on value-type field set" concern.
///
/// Theoretical concern: <see cref="BcsSharp.Core.Formatters.BcsObjectFormatter{T}"/>'s
/// value-type deserialize path uses <c>object boxedInstance = _constructor()</c> and
/// reflective <c>FieldInfo.SetValue</c>, and <c>ArgumentNullException.ThrowIfNull(value)</c>
/// in serialize would box a struct argument.
///
/// Measured reality on net11 P3: the struct and class paths allocate the same number
/// of bytes per op. The JIT erases the speculative struct-boxes, leaving only the
/// genuine per-field boxes (which happen on both paths because <c>IBcsObjectFormatter</c>
/// uses <c>object?</c> at the call boundary).
///
/// This test guards against future regressions where the gap reopens.
/// </summary>
public class BcsObjectFormatterAllocTests
{
    const int Iters = 100_000;

    private static double MeasureBytesPerOp(Action work)
    {
        for (int i = 0; i < 1_000; i++) work();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iters; i++) work();
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

        // After (a) guarding ArgumentNullException.ThrowIfNull behind !_isValueType and
        // (b) removing the GetOrAdd closure-capturing lambda from CompositeResolver,
        // struct and class allocate the same per op:
        //   Serialize   ~80 B/op = 24 B output byte[] + 48 B (2 uint field boxes) + ~8 B
        //   Deserialize ~72 B/op = 24 B instance box + 48 B (2 uint field boxes)
        // Remaining boxing is intrinsic to IBcsObjectFormatter's object? API; eliminating
        // it requires typed per-field delegates or a source generator.
        Assert.True(structSer <= classSer + 8, $"Struct serialize {structSer:F1} > class serialize {classSer:F1} + 8");
        Assert.True(structDeser <= classDeser + 8, $"Struct deserialize {structDeser:F1} > class deserialize {classDeser:F1} + 8");
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
