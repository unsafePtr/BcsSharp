using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// High-performance formatter for simple C-style enums (integer-backed enums).
/// BCS specification: variant index = ordinal position within the enum declaration
/// (the assigned discriminant values are ignored), encoded as ULEB128. For positions
/// 0-127 ULEB128 collapses to a single byte; positions 128+ produce 2+ bytes —
/// matching what Rust's <c>bcs</c> crate emits for unit-only enums.
///
/// Example: enum Status { Pending = 100, Active = 200, Disabled = 300 }
/// Status.Active serializes as ULEB128(1) = [0x01], not [0xC8] (assigned value 200)
/// and not [0x01] (which would also be correct for raw u8; the encoding diverges for
/// positions ≥ 128).
/// </summary>
/// <typeparam name="T">The enum type</typeparam>
public sealed class BcsSimpleEnumFormatter<T> : IBcsFormatter<T>, IBcsFormatter
    where T : struct, Enum
{
    // Static cache per generic type - this is efficient and appropriate for enum values
    private static readonly T[] _staticEnumValues = Enum.GetValues<T>();
    private static readonly int _enumCount = _staticEnumValues.Length;

    public Type TargetType => typeof(T);

    public void Serialize(ref BcsWriter writer, T value)
    {
        // BCS C-style enum serialization: Use ordinal position (index) within enum definition, not assigned values
        // This ensures consistent serialization regardless of actual enum values
        // Use cached enum values for optimal performance
        var position = Array.IndexOf(_staticEnumValues, value);

        if (position == -1)
        {
            throw new InvalidOperationException($"Enum value {value} not found in {typeof(T).Name}");
        }

        writer.WriteULEB((uint)position);
    }

    public T Deserialize(ref BcsReader reader)
    {
        var position = reader.ReadULEB32();

        if (position >= (uint)_enumCount)
        {
            throw new InvalidOperationException($"Invalid enum position {position} for {typeof(T).Name}. Enum has {_enumCount} values (0-{_enumCount - 1}).");
        }

        // Use cached enum values array for optimal performance (no reflection)
        return _staticEnumValues[(int)position];
    }
}

/// <summary>
/// Helper methods for simple enum formatting
/// </summary>
public static class BcsSimpleEnumHelper
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
