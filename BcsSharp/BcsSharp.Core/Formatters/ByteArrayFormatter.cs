namespace BcsSharp.Core.Formatters;

/// <summary>
/// Base class for fixed-length byte-array-backed types (e.g. Sui's 32-byte
/// <c>SuiAddress</c>). Subclasses provide the length, the byte view of an instance,
/// and a constructor from a span. Both Serialize and Deserialize are allocation-free.
/// </summary>
/// <example>
/// <code>
/// public sealed class SuiAddressFormatter : ByteArrayFormatter&lt;SuiAddress&gt;
/// {
///     public override int GetLength() =&gt; 32;
///     public override ReadOnlySpan&lt;byte&gt; GetBytes(SuiAddress value) =&gt; value.Bytes;
///     public override SuiAddress GetFromBytes(ReadOnlySpan&lt;byte&gt; bytes) =&gt; new(bytes);
/// }
/// </code>
/// </example>
public abstract class ByteArrayFormatter<T> : IBcsFormatter<T>
{
    public abstract int GetLength();
    public abstract ReadOnlySpan<byte> GetBytes(T value);
    public abstract T GetFromBytes(ReadOnlySpan<byte> bytes);

    public void Serialize(ref BcsWriter writer, T value)
    {
        writer.WritePrimitiveArray(GetBytes(value));
    }

    public T Deserialize(ref BcsReader reader)
    {
        return GetFromBytes(reader.ReadBytesAsSpan(GetLength()));
    }
}
