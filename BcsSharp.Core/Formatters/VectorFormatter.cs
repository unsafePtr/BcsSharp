namespace BcsSharp.Core.Formatters;

/// <summary>
/// List/Vector formatter for generic types.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ListFormatter<T> : IBcsFormatter<List<T>>
{
    private readonly IBcsFormatter<T> _elementFormatter;

    public ListFormatter(IBcsFormatter<T> elementFormatter)
    {
        _elementFormatter = elementFormatter;
    }

    public void Serialize(ref BcsWriter writer, List<T> value)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteULEB(0u);
            return;
        }

        writer.WriteULEB(unchecked((uint)value.Count));
        foreach (var item in value)
        {
            _elementFormatter.Serialize(ref writer, item);
        }
    }

    public List<T> Deserialize(ref BcsReader reader)
    {
        var length = reader.ReadLength();

        var result = new List<T>(InitialCapacity(length, ref reader));
        for (var i = 0; i < length; i++)
        {
            result.Add(_elementFormatter.Deserialize(ref reader));
        }

        return result;
    }

    public void Deserialize(ref BcsReader reader, ref List<T> value)
    {
        var length = reader.ReadLength();
        var capacity = InitialCapacity(length, ref reader);

        if (value is null)
        {
            value = new List<T>(capacity);
        }
        else
        {
            value.Clear();

            if (value.Capacity < capacity)
            {
                value.Capacity = capacity;
            }
        }

        for (var i = 0; i < length; i++)
        {
            value.Add(_elementFormatter.Deserialize(ref reader));
        }
    }

    /// <summary>
    /// Caps the up-front capacity at what the remaining input could fill, so a forged length cannot allocate more than the input could ever hold.
    /// Only the capacity is capped: an element can encode to zero bytes (a unit), so a length larger than the remaining input is valid.
    /// </summary>
    private static int InitialCapacity(int length, ref BcsReader reader) => Math.Min(length, reader.RemainingBytes);

}
