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

    /// <summary>
    /// Reconstruct a <typeparamref name="T"/> from its byte representation.
    /// Override this for backward compatibility / when the caller needs an owned array.
    /// </summary>
    public abstract T GetFromBytes(byte[] bytes);

    /// <summary>
    /// Zero-allocation deserialization hook. Override this when <typeparamref name="T"/> can
    /// be constructed directly from a span (e.g. copying into a fixed-size struct field).
    /// The default forwards to <see cref="GetFromBytes(byte[])"/> via one <c>ToArray()</c>
    /// copy, preserving the behavior of subclasses that only implement the byte[] overload.
    /// </summary>
    public virtual T GetFromBytes(ReadOnlySpan<byte> bytes) => GetFromBytes(bytes.ToArray());


    public void Serialize(ref BcsWriter writer, T value)
    {
        writer.WritePrimitiveArray(GetBytes(value));
    }

    public T Deserialize(ref BcsReader reader)
    {
        // Read the bytes as a span over the input buffer (no copy). Subclasses that override
        // GetFromBytes(ReadOnlySpan<byte>) deserialize allocation-free; subclasses that only
        // override GetFromBytes(byte[]) get the same one-allocation cost they had before.
        var span = reader.ReadBytesAsSpan(GetLength());
        return GetFromBytes(span);
    }
}
