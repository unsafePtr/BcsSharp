namespace BcsSharp.Core.Formatters;

public abstract class PrimitiveArrayFormatter<T> : IBcsFormatter<T[]>
    where T : unmanaged
{
    public abstract int GetLength();

    public void Serialize(ref BcsWriter writer, T[] value)
    {
        writer.WritePrimitiveArray<T>(value.AsSpan());
    }

    public T[] Deserialize(ref BcsReader reader)
    {
        var result = new T[GetLength()];
        reader.ReadPrimitiveArray(result.AsSpan());
        return result;
    }

    public int? GetSerializedSize(T[] value)
    {
        throw new NotImplementedException();
    }
}
