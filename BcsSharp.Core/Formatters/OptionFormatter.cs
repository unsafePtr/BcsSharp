namespace BcsSharp.Core.Formatters;

internal static class OptionFormatterCache
{
    public static readonly OptionFormatter<byte> ByteOptionFormatter = new OptionFormatter<byte>(ByteFormatter.Instance);
    public static readonly OptionFormatter<sbyte> SByteOptionFormatter = new OptionFormatter<sbyte>(SByteFormatter.Instance);
    public static readonly OptionFormatter<ushort> UInt16OptionFormatter = new OptionFormatter<ushort>(UInt16Formatter.Instance);
    public static readonly OptionFormatter<short> Int16OptionFormatter = new OptionFormatter<short>(Int16Formatter.Instance);
    public static readonly OptionFormatter<uint> UInt32OptionFormatter = new OptionFormatter<uint>(UInt32Formatter.Instance);
    public static readonly OptionFormatter<int> Int32OptionFormatter = new OptionFormatter<int>(Int32Formatter.Instance);
    public static readonly OptionFormatter<ulong> UInt64OptionFormatter = new OptionFormatter<ulong>(UInt64Formatter.Instance);
    public static readonly OptionFormatter<long> Int64OptionFormatter = new OptionFormatter<long>(Int64Formatter.Instance);
    public static readonly OptionFormatter<UInt128> UInt128OptionFormatter = new OptionFormatter<UInt128>(UInt128Formatter.Instance);
    public static readonly OptionFormatter<Int128> Int128OptionFormatter = new OptionFormatter<Int128>(Int128Formatter.Instance);
    public static readonly OptionFormatter<bool> BoolOptionFormatter = new OptionFormatter<bool>(BoolFormatter.Instance);
}

/// <summary>
/// BCS <c>Option&lt;T&gt;</c> over a value type: <c>0x00</c> for None, <c>0x01</c> followed by the payload for Some.
/// Any other discriminant is rejected rather than treated as Some.
/// </summary>
public sealed class OptionFormatter<T> : IBcsFormatter<T?> where T : struct
{
    private readonly IBcsFormatter<T> _valueFormatter;

    public OptionFormatter(IBcsFormatter<T> valueFormatter)
    {
        _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
    }

    public void Serialize(ref BcsWriter writer, T? value)
    {
        if (value.HasValue)
        {
            // Write 1 to indicate Some(value)
            writer.Write((byte)1);
            _valueFormatter.Serialize(ref writer, value.Value);
        }
        else
        {
            // Write 0 to indicate None
            writer.Write((byte)0);
        }
    }

    public T? Deserialize(ref BcsReader reader)
    {
        var hasValue = reader.Read8();
        if (hasValue == 0)
        {
            return null;
        }
        else if (hasValue == 1)
        {
            return _valueFormatter.Deserialize(ref reader);
        }
        else
        {
            throw new InvalidOperationException($"Invalid Option discriminant: {hasValue}. Expected 0 (None) or 1 (Some).");
        }
    }

}
