using System.Buffers;
using System.Runtime.CompilerServices;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// Per-op allocation guards for the <c>union</c> path — <c>Option&lt;T&gt;</c> and multi-case unions alike.
///
/// Serialization into an <see cref="IBufferWriter{T}"/> must be literally allocation-free, and deserialization must allocate the payload instance and nothing else.
/// Two regressions this pins down: a unit case (<c>None</c>) used to construct a fresh instance per read, and a struct payload used to be boxed twice — once by <c>IBcsObjectFormatter.DeserializeObject</c>, once by the union constructor.
/// </summary>
public class UnionFormatterAllocTests
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
    public void Serialize_IntoBufferWriter_IsZeroAlloc()
    {
        var buffer = new ArrayBufferWriter<byte>(64);

        Option<Zip> some = new Zip { Code = 1000 };
        Option<Zip> none = None.Instance;
        Option<string> someString = "sofia";
        Coordinate boxed = new Point { X = 1, Y = 2 };

        var someAlloc = MeasureBytesPerOp(() => { buffer.ResetWrittenCount(); BcsSerializer.Serialize(buffer, some); });
        var noneAlloc = MeasureBytesPerOp(() => { buffer.ResetWrittenCount(); BcsSerializer.Serialize(buffer, none); });
        var stringAlloc = MeasureBytesPerOp(() => { buffer.ResetWrittenCount(); BcsSerializer.Serialize(buffer, someString); });
        var structAlloc = MeasureBytesPerOp(() => { buffer.ResetWrittenCount(); BcsSerializer.Serialize(buffer, boxed); });

        Console.WriteLine($"serialize Some(class):  {someAlloc,6:F1} B/op");
        Console.WriteLine($"serialize None:         {noneAlloc,6:F1} B/op");
        Console.WriteLine($"serialize Some(string): {stringAlloc,6:F1} B/op");
        Console.WriteLine($"serialize Some(struct): {structAlloc,6:F1} B/op");

        Assert.Equal(0.0, someAlloc);
        Assert.Equal(0.0, noneAlloc);
        Assert.Equal(0.0, stringAlloc);
        Assert.Equal(0.0, structAlloc);
    }

    [Fact]
    public void Deserialize_None_IsZeroAlloc()
    {
        var bytes = BcsSerializer.Serialize<Option<Zip>>(None.Instance);

        var perOp = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<Option<Zip>>(bytes));

        Console.WriteLine($"deserialize None:       {perOp,6:F1} B/op");
        Assert.Equal(0.0, perOp);
    }

    [Fact]
    public void Deserialize_None_ReturnsTheSingleton()
    {
        var bytes = BcsSerializer.Serialize<Option<Zip>>(None.Instance);

        var back = BcsSerializer.Deserialize<Option<Zip>>(bytes);

        Assert.Same(None.Instance, ((IUnion)back).Value);
    }

    [Fact]
    public void Deserialize_ClassPayload_AllocatesOnlyTheInstance()
    {
        var bytes = BcsSerializer.Serialize<Option<Zip>>(new Zip { Code = 1000 });

        var perOp = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<Option<Zip>>(bytes));

        Console.WriteLine($"deserialize Some(class): {perOp,6:F1} B/op");
        Assert.True(perOp <= 24, $"Expected the Zip instance and nothing more, got {perOp:F1} B/op");
    }

    [Fact]
    public void Deserialize_StructPayload_BoxesOnce()
    {
        var bytes = BcsSerializer.Serialize<Coordinate>(new Point { X = 1, Y = 2 });

        var perOp = MeasureBytesPerOp(() => _ = BcsSerializer.Deserialize<Coordinate>(bytes));

        // A union stores its payload in an object slot, so one box is structural.
        // The second box (the object? return of the case formatter) is not, and must stay gone.
        Console.WriteLine($"deserialize Some(struct): {perOp,6:F1} B/op");
        Assert.True(perOp <= 24, $"Expected a single box, got {perOp:F1} B/op");
    }

    // --- Fixtures -------------------------------------------------------------

    [BcsStruct]
    public sealed class Zip
    {
        [BcsField(0)] public uint Code { get; set; }
    }

    [BcsStruct]
    public struct Point
    {
        [BcsField(0)] public uint X { get; set; }
        [BcsField(1)] public uint Y { get; set; }
    }

    public union Coordinate(None, Point);
}
