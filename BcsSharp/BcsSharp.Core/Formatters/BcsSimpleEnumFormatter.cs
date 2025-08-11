using System;
using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Extensions;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// High-performance formatter for simple C-style enums (integer-backed enums).
    /// Uses cached enum values arrays to avoid repeated reflection calls.
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
        private readonly T[] _cachedEnumValues;
        private readonly int _enumCount;

        // Static cache for enum values to avoid repeated Enum.GetValues calls
        private static readonly Dictionary<Type, T[]> _enumValuesCache = new();

        public Type TargetType => typeof(T);

        public BcsSimpleEnumFormatter()
        {
            // Always initialize byte formatter since we serialize ordinal positions as bytes
            _byteFormatter = ByteFormatter.Instance;

            // Cache enum values for high-performance access
            var enumValues = GetOrCreateCachedEnumValues(typeof(T));
            _cachedEnumValues = enumValues;
            _enumCount = _cachedEnumValues.Length;

            if (_enumCount > 256)
            {
                throw new InvalidOperationException($"Enum {typeof(T).Name} has too many values ({_enumCount}). BCS C-style enums support maximum 256 values (0-255).");
            }
        }

        public void Serialize(ref BcsWriter writer, T value)
        {
            // BCS C-style enum serialization: Use ordinal position (index) within enum definition, not assigned values
            // This ensures consistent serialization regardless of actual enum values
            // Use cached enum values for optimal performance
            var position = Array.IndexOf(_cachedEnumValues, value);

            if (position == -1)
            {
                throw new InvalidOperationException($"Enum value {value} not found in {typeof(T).Name}");
            }

            // Always serialize as byte representing the ordinal position
            _byteFormatter.Serialize(ref writer, (byte)position);
        }

        public T Deserialize(ref BcsReader reader)
        {
            // BCS C-style enum deserialization: Read ordinal position and convert to enum value
            var position = _byteFormatter.Deserialize(ref reader);

            if (position >= _enumCount)
            {
                throw new InvalidOperationException($"Invalid enum position {position} for {typeof(T).Name}. Enum has {_enumCount} values (0-{_enumCount - 1}).");
            }

            // Use cached enum values array for optimal performance (no reflection)
            return _cachedEnumValues[position];
        }

        public int? GetSerializedSize(T value)
        {
            // BCS C-style enums always serialize as 1 byte (ordinal position)
            return 1;
        }

        /// <summary>
        /// Gets or creates cached enum values for a specific enum type to avoid repeated reflection
        /// </summary>
        private static T[] GetOrCreateCachedEnumValues(Type enumType)
        {
            return _enumValuesCache.GetOrAddWithLock(enumType, type => (Enum.GetValues(type)! as T[])!);
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