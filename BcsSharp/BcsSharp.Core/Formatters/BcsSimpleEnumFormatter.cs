using System;
using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// Formatter for simple C-style enums (integer-backed enums).
    /// BCS specification: Serializes based on ordinal position (index) within enum definition, not assigned values.
    /// Always serializes as a single byte representing the position (0-255).
    /// 
    /// Example: enum Status { Pending = 100, Active = 200, Disabled = 300 }
    /// Status.Active serializes as byte 1 (position 1, counting from 0), not 200 (assigned value)
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    public sealed class BcsSimpleEnumFormatter<T> : IBcsFormatter<T>, IBcsFormatter
        where T : struct, Enum
    {
        private readonly IBcsFormatter<byte> _byteFormatter;

        public Type TargetType => typeof(T);

        public BcsSimpleEnumFormatter()
        {
            // Always initialize byte formatter since we serialize ordinal positions as bytes
            _byteFormatter = ByteFormatter.Instance;
        }

        public void Serialize(ref BcsWriter writer, T value)
        {
            // BCS C-style enum serialization: Use ordinal position (index) within enum definition, not assigned values
            // This ensures consistent serialization regardless of actual enum values
            var enumValues = Enum.GetValues(typeof(T));
            var position = Array.IndexOf(enumValues, value);

            if (position == -1)
            {
                throw new InvalidOperationException($"Enum value {value} not found in {typeof(T).Name}");
            }

            if (position > 255)
            {
                throw new InvalidOperationException($"Enum {typeof(T).Name} has too many values ({position + 1}). BCS C-style enums support maximum 256 values (0-255).");
            }

            // Always serialize as byte representing the ordinal position
            _byteFormatter.Serialize(ref writer, (byte)position);
        }

        public T Deserialize(ref BcsReader reader)
        {
            // BCS C-style enum deserialization: Read ordinal position and convert to enum value
            var position = _byteFormatter.Deserialize(ref reader);
            var enumValues = Enum.GetValues(typeof(T));

            if (position >= enumValues.Length)
            {
                throw new InvalidOperationException($"Invalid enum position {position} for {typeof(T).Name}. Enum has {enumValues.Length} values (0-{enumValues.Length - 1}).");
            }

            return (T)enumValues.GetValue(position)!;
        }

        public int? GetSerializedSize(T value)
        {
            // BCS C-style enums always serialize as 1 byte (ordinal position)
            return 1;
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
}