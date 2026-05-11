using System;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// Formatter for 2-tuple (T1, T2)
/// </summary>
public sealed class TupleFormatter<T1, T2> : IBcsFormatter<(T1, T2)>
{
    private readonly IBcsFormatter<T1> _item1Formatter;
    private readonly IBcsFormatter<T2> _item2Formatter;

    public Type TargetType => typeof((T1, T2));

    public TupleFormatter(IBcsFormatter<T1> item1Formatter, IBcsFormatter<T2> item2Formatter)
    {
        _item1Formatter = item1Formatter ?? throw new ArgumentNullException(nameof(item1Formatter));
        _item2Formatter = item2Formatter ?? throw new ArgumentNullException(nameof(item2Formatter));
    }

    public void Serialize(ref BcsWriter writer, (T1, T2) value)
    {
        _item1Formatter.Serialize(ref writer, value.Item1);
        _item2Formatter.Serialize(ref writer, value.Item2);
    }

    public (T1, T2) Deserialize(ref BcsReader reader)
    {
        var item1 = _item1Formatter.Deserialize(ref reader);
        var item2 = _item2Formatter.Deserialize(ref reader);
        return (item1, item2);
    }

}

/// <summary>
/// Formatter for 3-tuple (T1, T2, T3)
/// </summary>
public sealed class TupleFormatter<T1, T2, T3> : IBcsFormatter<(T1, T2, T3)>
{
    private readonly IBcsFormatter<T1> _item1Formatter;
    private readonly IBcsFormatter<T2> _item2Formatter;
    private readonly IBcsFormatter<T3> _item3Formatter;

    public Type TargetType => typeof((T1, T2, T3));

    public TupleFormatter(IBcsFormatter<T1> item1Formatter, IBcsFormatter<T2> item2Formatter, IBcsFormatter<T3> item3Formatter)
    {
        _item1Formatter = item1Formatter ?? throw new ArgumentNullException(nameof(item1Formatter));
        _item2Formatter = item2Formatter ?? throw new ArgumentNullException(nameof(item2Formatter));
        _item3Formatter = item3Formatter ?? throw new ArgumentNullException(nameof(item3Formatter));
    }

    public void Serialize(ref BcsWriter writer, (T1, T2, T3) value)
    {
        _item1Formatter.Serialize(ref writer, value.Item1);
        _item2Formatter.Serialize(ref writer, value.Item2);
        _item3Formatter.Serialize(ref writer, value.Item3);
    }

    public (T1, T2, T3) Deserialize(ref BcsReader reader)
    {
        var item1 = _item1Formatter.Deserialize(ref reader);
        var item2 = _item2Formatter.Deserialize(ref reader);
        var item3 = _item3Formatter.Deserialize(ref reader);
        return (item1, item2, item3);
    }

}

/// <summary>
/// Formatter for 4-tuple (T1, T2, T3, T4)
/// </summary>
public sealed class TupleFormatter<T1, T2, T3, T4> : IBcsFormatter<(T1, T2, T3, T4)>
{
    private readonly IBcsFormatter<T1> _item1Formatter;
    private readonly IBcsFormatter<T2> _item2Formatter;
    private readonly IBcsFormatter<T3> _item3Formatter;
    private readonly IBcsFormatter<T4> _item4Formatter;

    public Type TargetType => typeof((T1, T2, T3, T4));

    public TupleFormatter(IBcsFormatter<T1> item1Formatter, IBcsFormatter<T2> item2Formatter, IBcsFormatter<T3> item3Formatter, IBcsFormatter<T4> item4Formatter)
    {
        _item1Formatter = item1Formatter ?? throw new ArgumentNullException(nameof(item1Formatter));
        _item2Formatter = item2Formatter ?? throw new ArgumentNullException(nameof(item2Formatter));
        _item3Formatter = item3Formatter ?? throw new ArgumentNullException(nameof(item3Formatter));
        _item4Formatter = item4Formatter ?? throw new ArgumentNullException(nameof(item4Formatter));
    }

    public void Serialize(ref BcsWriter writer, (T1, T2, T3, T4) value)
    {
        _item1Formatter.Serialize(ref writer, value.Item1);
        _item2Formatter.Serialize(ref writer, value.Item2);
        _item3Formatter.Serialize(ref writer, value.Item3);
        _item4Formatter.Serialize(ref writer, value.Item4);
    }

    public (T1, T2, T3, T4) Deserialize(ref BcsReader reader)
    {
        var item1 = _item1Formatter.Deserialize(ref reader);
        var item2 = _item2Formatter.Deserialize(ref reader);
        var item3 = _item3Formatter.Deserialize(ref reader);
        var item4 = _item4Formatter.Deserialize(ref reader);
        return (item1, item2, item3, item4);
    }

}
