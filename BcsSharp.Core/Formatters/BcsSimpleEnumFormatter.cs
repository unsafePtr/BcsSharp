using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// Formatter for plain CLR enums, the C# shape of a unit-only Rust enum.
/// The wire carries the ULEB128 of the member's position in the enum declaration, as serde ignores explicit discriminants: in <c>enum Status { Pending = 100, Active = 200 }</c>, <c>Active</c> is <c>[0x01]</c>.
/// Positions 0-127 collapse to one byte; 128+ take two or more.
/// <c>Enum.GetValues</c> cannot supply that order because it sorts by value; the enum's fields come back in declaration order instead.
/// Two members with the same value are rejected: the wire has one index per member, so the alias could not round-trip.
/// </summary>
/// <typeparam name="T">The enum type</typeparam>
public sealed class BcsSimpleEnumFormatter<T> : IBcsFormatter<T>
    where T : struct, Enum
{
    private readonly T[] _declaredValues = ReadDeclaredValues();

    public void Serialize(ref BcsWriter writer, T value)
    {
        var position = Array.IndexOf(_declaredValues, value);

        if (position == -1)
        {
            throw new InvalidOperationException($"Enum value {value} not found in {typeof(T).Name}");
        }

        writer.WriteULEB((uint)position);
    }

    public T Deserialize(ref BcsReader reader)
    {
        // Rust counts a unit variant as a container too, so an enum at the depth limit is rejected the same way.
        reader.EnterContainer();

        var position = reader.ReadULEB32();

        if (position >= (uint)_declaredValues.Length)
        {
            throw new InvalidOperationException($"Invalid enum position {position} for {typeof(T).Name}. Enum has {_declaredValues.Length} values (0-{_declaredValues.Length - 1}).");
        }

        reader.LeaveContainer();

        return _declaredValues[(int)position];
    }

    private static T[] ReadDeclaredValues()
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);
        var values = new T[fields.Length];

        for (var i = 0; i < fields.Length; i++)
        {
            var value = (T)fields[i].GetValue(null)!;

            var alias = Array.IndexOf(values, value, 0, i);
            if (alias >= 0)
            {
                throw new InvalidOperationException($"Enum {typeof(T).Name} declares {fields[alias].Name} and {fields[i].Name} with the same value, which BCS cannot encode as two variants.");
            }

            values[i] = value;
        }

        return values;
    }
}

/// <summary>
/// Helper methods for simple enum formatting
/// </summary>
internal static class BcsSimpleEnumHelper
{
    /// <summary>
    /// Checks if a type is a simple C-style enum (not marked with [BcsEnum] for variants)
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if it's a simple enum</returns>
    public static bool IsSimpleEnum(Type type)
    {
        return type.IsEnum && type.GetCustomAttribute<BcsEnumAttribute>() == null;
    }
}
