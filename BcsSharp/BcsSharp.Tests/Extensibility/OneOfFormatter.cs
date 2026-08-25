using BcsSharp.Core;
using OneOf;
using OneOf.Types;

namespace BcsSharp.Tests.Extensibility;

/// <summary>
/// Consumer-side formatter for <c>OneOf&lt;None, T&gt;</c> as a BCS <c>Option&lt;T&gt;</c>:
/// discriminant byte 0 for None, 1 followed by the payload for Some(T).
/// </summary>
/// <remarks>
/// The library's own optional type is the C# 15 <c>union Option&lt;T&gt;</c> served by
/// <c>UnionResolver</c>; OneOf was never wired into <see cref="Core.Resolvers.CompositeResolver.Default"/>,
/// so shipping it only forced the OneOf package on every consumer. It lives here as the
/// worked example for projects that already model optionals with OneOf — register it on
/// <c>CustomFormatterResolver</c> at startup, per closed generic.
/// See <see cref="OneOfManualRegistrationTests"/>, which asserts the wire output is
/// byte-identical to the union path.
/// </remarks>
public sealed class OneOfFormatter<T> : IBcsFormatter<OneOf<None, T>>
{
    private readonly IBcsFormatter<T> _valueFormatter;

    public OneOfFormatter(IBcsFormatter<T> valueFormatter)
    {
        _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
    }

    public void Serialize(ref BcsWriter writer, OneOf<None, T> value)
    {
        if (value.IsT0)
        {
            writer.Write((byte)0);
            return;
        }

        if (!value.IsT1)
        {
            throw new InvalidOperationException("OneOf<None, T> is in an invalid state");
        }

        writer.Write((byte)1);
        _valueFormatter.Serialize(ref writer, value.AsT1);
    }

    public OneOf<None, T> Deserialize(ref BcsReader reader)
    {
        var discriminant = reader.Read8();
        return discriminant switch
        {
            0 => new None(),
            1 => _valueFormatter.Deserialize(ref reader),
            _ => throw new InvalidOperationException(
                $"Invalid OneOf discriminant: {discriminant}. Expected 0 (None) or 1 (Some)."),
        };
    }
}
