using System.Runtime.InteropServices;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// List/Vector formatter for primitive types using vectorized operations
/// </summary>
public sealed class PrimitiveListFormatter<T> : IBcsFormatter<List<T>> where T : unmanaged
{
    public Type TargetType => typeof(List<T>);

    public static PrimitiveListFormatter<T> GetInstance()
    {
        return new PrimitiveListFormatter<T>();
    }

    public void Serialize(ref BcsWriter writer, List<T> value)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteULEB(0u);
            return;
        }

        writer.WriteULEB(unchecked((uint)value.Count));
        var span = CollectionsMarshal.AsSpan(value);
        writer.WritePrimitiveArray<T>(span);
    }

    public List<T> Deserialize(ref BcsReader reader)
    {
        var length = reader.ReadULEB32();
        if (length == 0)
        {
            return new List<T>(0);
        }

        var count = unchecked((int)length);
        var result = new List<T>(count);
        CollectionsMarshal.SetCount(result, count);
        var span = CollectionsMarshal.AsSpan(result);
        reader.ReadPrimitiveArray(span);
        return result;
    }

    public void Deserialize(ref BcsReader reader, ref List<T> value)
    {
        var count = unchecked((int)reader.ReadULEB32());

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
            reader.ReadPrimitiveArray(CollectionsMarshal.AsSpan(value));
        }
    }


}
