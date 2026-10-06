using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// List/Vector formatter for primitive types using vectorized operations
/// </summary>
public sealed class PrimitiveListFormatter<T> : IBcsFormatter<List<T>> where T : unmanaged
{

    public void Serialize(ref BcsWriter writer, List<T> value)
    {
        if (value is null)
        {
            ThrowHelper.ThrowNullVector(nameof(value));
        }

        var count = value.Count;
        writer.WriteULEB((uint)count);
        if (count == 0)
        {
            return;
        }

        writer.WritePrimitiveArray<T>(CollectionsMarshal.AsSpan(value));
    }

    public List<T> Deserialize(ref BcsReader reader)
    {
        var count = ReadCount(ref reader);

        if (count == 0)
        {
            return new List<T>(0);
        }

        var result = new List<T>(count);
        CollectionsMarshal.SetCount(result, count);

        var span = CollectionsMarshal.AsSpan(result);
        reader.ReadPrimitiveArray(span);
        ThrowIfInvalidBools(span);

        return result;
    }

    public void Deserialize(ref BcsReader reader, ref List<T> value)
    {
        var count = ReadCount(ref reader);

        if (value is null)
        {
            value = new List<T>(count);
        }
        else if (value.Capacity < count)
        {
            value.Capacity = count;
        }

        CollectionsMarshal.SetCount(value, count);
        if (count > 0)
        {
            var span = CollectionsMarshal.AsSpan(value);
            reader.ReadPrimitiveArray(span);
            ThrowIfInvalidBools(span);
        }
    }

    // The bulk copy bypasses ReadBool, so a bool list gets the same 0-or-1 check here; for every other element type the JIT drops the branch.
    private static void ThrowIfInvalidBools(ReadOnlySpan<T> values)
    {
        if (typeof(T) != typeof(bool))
        {
            return;
        }

        var bytes = MemoryMarshal.AsBytes(values);
        var invalid = bytes.IndexOfAnyExcept((byte)0, (byte)1);

        if (invalid >= 0)
        {
            ThrowHelper.ThrowInvalidOperationException($"Invalid boolean value: {bytes[invalid]}. Expected 0 or 1.");
        }
    }

    // Every element has the same fixed size, so a length the remaining input cannot hold is rejected before anything is allocated.
    private static int ReadCount(ref BcsReader reader)
    {
        var count = reader.ReadLength();
        var byteCount = (long)count * Unsafe.SizeOf<T>();

        if (byteCount > reader.RemainingBytes)
        {
            ThrowHelper.ThrowEndOfStreamException(byteCount);
        }

        return count;
    }

}
