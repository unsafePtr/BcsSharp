namespace BcsSharp.Core.Formatters;

/// <summary>
/// List/Vector formatter for generic types.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ListFormatter<T> : IBcsFormatter<List<T>>
{
    private readonly IBcsFormatter<T> _elementFormatter;

    public Type TargetType => typeof(List<T>);

    public ListFormatter(IBcsFormatter<T> elementFormatter)
    {
        _elementFormatter = elementFormatter;
    }

    public static ListFormatter<T> GetInstance(IBcsFormatter<T> elementFormatter)
    {
        // Captures elementFormatter, so it belongs to the chain that resolved it; the root
        // chain owns the cache.
        return new ListFormatter<T>(elementFormatter);
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
        var length = reader.ReadULEB32();
        if (length == 0)
        {
            return new List<T>(0);
        }

        var result = new List<T>(unchecked((int)length));
        for (int i = 0; i < length; i++)
        {
            result.Add(_elementFormatter.Deserialize(ref reader));
        }

        return result;
    }

    public void Deserialize(ref BcsReader reader, ref List<T> value)
    {
        var length = unchecked((int)reader.ReadULEB32());

        if (value is null)
        {
            value = new List<T>(length);
        }
        else
        {
            value.Clear();
            if (value.Capacity < length)
            {
                value.Capacity = length;
            }
        }

        for (int i = 0; i < length; i++)
        {
            value.Add(_elementFormatter.Deserialize(ref reader));
        }
    }


}
