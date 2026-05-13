using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// In-place <c>Deserialize&lt;T&gt;(span, ref T)</c> reuses the caller's instance /
/// collection rather than allocating fresh. Strings are unavoidable allocations
/// (immutable, can't be mutated in place), so the "zero alloc" tests use only
/// primitive-typed targets.
/// </summary>
public class InPlaceDeserializeTests
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
    public void InPlace_ClassWithPrimitives_ReusesInstance_ZeroAlloc()
    {
        var original = new Coords { Id = 12345, X = 30, Y = 99 };
        var bytes = BcsSerializer.Serialize(original);

        var dest = new Coords();
        // Capture the reference; in-place must not replace it.
        var destBefore = dest;

        var perOp = MeasureBytesPerOp(() => BcsSerializer.Deserialize(bytes, ref dest));

        Console.WriteLine($"Coords in-place deserialize: {perOp:F1} B/op");

        Assert.Same(destBefore, dest);
        Assert.Equal(12345UL, dest.Id);
        Assert.Equal(30u, dest.X);
        Assert.Equal(99u, dest.Y);
        Assert.True(perOp < 0.01, $"Expected ~0 B/op, got {perOp}");
    }

    [Fact]
    public void InPlace_NullClassTarget_AllocatesFresh()
    {
        var original = new Coords { Id = 1, X = 2, Y = 3 };
        var bytes = BcsSerializer.Serialize(original);

        Coords? dest = null;
        BcsSerializer.Deserialize(bytes, ref dest);

        Assert.NotNull(dest);
        Assert.Equal(1UL, dest.Id);
    }

    [Fact]
    public void InPlace_StructTarget_WritesDirectlyIntoCallerStorage()
    {
        var original = new Point { X = 7, Y = 13 };
        var bytes = BcsSerializer.Serialize(original);

        Point dest = default;
        BcsSerializer.Deserialize(bytes, ref dest);

        Assert.Equal(7u, dest.X);
        Assert.Equal(13u, dest.Y);
    }

    [Fact]
    public void InPlace_ListField_ReusesContainer()
    {
        var original = new Holder { Numbers = [1, 2, 3, 4, 5] };
        var bytes = BcsSerializer.Serialize(original);

        var dest = new Holder { Numbers = new List<uint>(16) };
        var listBefore = dest.Numbers;

        BcsSerializer.Deserialize(bytes, ref dest);

        Assert.Same(listBefore, dest.Numbers);
        Assert.Equal(new List<uint> { 1, 2, 3, 4, 5 }, dest.Numbers);
    }

    [Fact]
    public void InPlace_DictionaryField_ReusesContainer()
    {
        var original = new MapHolder
        {
            Map = new Dictionary<uint, uint> { [1] = 10, [2] = 20, [3] = 30 },
        };
        var bytes = BcsSerializer.Serialize(original);

        var dest = new MapHolder { Map = new Dictionary<uint, uint>() };
        var dictBefore = dest.Map;

        BcsSerializer.Deserialize(bytes, ref dest);

        Assert.Same(dictBefore, dest.Map);
        Assert.Equal(3, dest.Map.Count);
        Assert.Equal(10u, dest.Map[1]);
        Assert.Equal(20u, dest.Map[2]);
        Assert.Equal(30u, dest.Map[3]);
    }

    [Fact]
    public void InPlace_ClassWithStringField_StillRoundTrips()
    {
        // Strings allocate on each deserialize (immutable), but the class instance itself
        // is reused — only the field-value allocations remain.
        var original = new User { Id = 1, Name = "Alice" };
        var bytes = BcsSerializer.Serialize(original);

        var dest = new User();
        var destBefore = dest;

        BcsSerializer.Deserialize(bytes, ref dest);

        Assert.Same(destBefore, dest);
        Assert.Equal(1UL, dest.Id);
        Assert.Equal("Alice", dest.Name);
    }

    // --- Fixtures -----------------------------------------------------------

    [BcsStruct]
    public sealed class Coords
    {
        [BcsField(0)] public ulong Id { get; set; }
        [BcsField(1)] public uint X { get; set; }
        [BcsField(2)] public uint Y { get; set; }
    }

    [BcsStruct]
    public sealed class User
    {
        [BcsField(0)] public ulong Id { get; set; }
        [BcsField(1)] public string Name { get; set; } = "";
    }

    [BcsStruct]
    public struct Point
    {
        [BcsField(0)] public uint X { get; set; }
        [BcsField(1)] public uint Y { get; set; }
    }

    [BcsStruct]
    public sealed class Holder
    {
        [BcsField(0)] public List<uint> Numbers { get; set; } = [];
    }

    [BcsStruct]
    public sealed class MapHolder
    {
        [BcsField(0)] public Dictionary<uint, uint> Map { get; set; } = [];
    }
}
