using System;
using OneOf;
using OneOf.Types;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// Formatter for OneOf&lt;None, T&gt; type - handles Rust Option&lt;T&gt; equivalent
    /// Serializes as BCS Option with discriminant byte: 0 for None, 1 for Some(T)
    /// </summary>
    public sealed class OneOfFormatter<T> : IBcsFormatter<OneOf<None, T>>
    {
        private readonly IBcsFormatter<T> _valueFormatter;

        public Type TargetType => typeof(OneOf<None, T>);

        public OneOfFormatter(IBcsFormatter<T> valueFormatter)
        {
            _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
        }

        public void Serialize(ref BcsWriter writer, OneOf<None, T> value)
        {
            if (value.IsT0) // None
            {
                // Write 0 to indicate None
                writer.Write((byte)0);
            }
            else if (value.IsT1) // Some(T)
            {
                // Write 1 to indicate Some(value)
                writer.Write((byte)1);
                _valueFormatter.Serialize(ref writer, value.AsT1);
            }
            else
            {
                throw new InvalidOperationException("OneOf<None, T> is in an invalid state");
            }
        }

        public OneOf<None, T> Deserialize(ref BcsReader reader)
        {
            var discriminant = reader.Read8();
            if (discriminant == 0)
            {
                return new None();
            }
            else if (discriminant == 1)
            {
                var value = _valueFormatter.Deserialize(ref reader);
                return value;
            }
            else
            {
                throw new InvalidOperationException($"Invalid OneOf discriminant: {discriminant}. Expected 0 (None) or 1 (Some).");
            }
        }

        public int? GetSerializedSize(OneOf<None, T> value)
        {
            return value.Match<int?>(
                none => 1, // Just the discriminant byte
                some => 
                {
                    var valueSize = _valueFormatter.GetSerializedSize(some);
                    if (valueSize == null)
                        return null; // Variable size
                    return 1 + valueSize.Value; // Discriminant + value
                });
        }
    }
}