namespace BcsSharp.Core.Formatters;

/// <summary>
/// Generic class for formatting primitive arrays in BCS serialization.
/// You should implement this class for each primitive type you want to support.
/// Such can be SuiAddress - `public class SuiAddressFormatter : ByteArrayFormatter<SuiAddress>`
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ByteArrayFormatter<T> : IBcsFormatter<T>
{
    public abstract int GetLength();
    public abstract ReadOnlySpan<byte> GetBytes(T value);
    public abstract T GetFromBytes(byte[] bytes);


    public void Serialize(ref BcsWriter writer, T value)
    {
        writer.WritePrimitiveArray(GetBytes(value));
    }

    public T Deserialize(ref BcsReader reader)
    {
        var result = new byte[GetLength()];
        reader.ReadPrimitiveArray(result.AsSpan());

        return GetFromBytes(result);
    }
}
