using System;
using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// Formatter for simple C-style enums (integer-backed enums).
    /// Serializes the underlying integer value directly using the enum's underlying type.
    /// 
    /// Example: enum Status { Pending = 0, Active = 1, Disabled = 2 }
    /// Status.Active serializes as the integer value 1
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    public sealed class BcsSimpleEnumFormatter<T> : IBcsFormatter<T>, IBcsFormatter
        where T : struct, Enum
    {
        private readonly Type _underlyingType;
        private readonly IBcsFormatter<byte>? _byteFormatter;
        private readonly IBcsFormatter<ushort>? _ushortFormatter;
        private readonly IBcsFormatter<uint>? _uintFormatter;
        private readonly IBcsFormatter<ulong>? _ulongFormatter;
        private readonly IBcsFormatter<sbyte>? _sbyteFormatter;
        private readonly IBcsFormatter<short>? _shortFormatter;
        private readonly IBcsFormatter<int>? _intFormatter;
        private readonly IBcsFormatter<long>? _longFormatter;

        public Type TargetType => typeof(T);

        public BcsSimpleEnumFormatter()
        {
            _underlyingType = Enum.GetUnderlyingType(typeof(T));
            
            // Initialize the appropriate formatter based on underlying type
            if (_underlyingType == typeof(byte))
                _byteFormatter = ByteFormatter.Instance;
            else if (_underlyingType == typeof(ushort))
                _ushortFormatter = UInt16Formatter.Instance;
            else if (_underlyingType == typeof(uint))
                _uintFormatter = UInt32Formatter.Instance;
            else if (_underlyingType == typeof(ulong))
                _ulongFormatter = UInt64Formatter.Instance;
            else if (_underlyingType == typeof(sbyte))
                _sbyteFormatter = SByteFormatter.Instance;
            else if (_underlyingType == typeof(short))
                _shortFormatter = Int16Formatter.Instance;
            else if (_underlyingType == typeof(int))
                _intFormatter = Int32Formatter.Instance;
            else if (_underlyingType == typeof(long))
                _longFormatter = Int64Formatter.Instance;
            else
                throw new NotSupportedException($"Underlying enum type {_underlyingType.Name} is not supported");
        }

        public void Serialize(ref BcsWriter writer, T value)
        {
            // Convert enum to its underlying type and serialize
            var underlyingValue = Convert.ChangeType(value, _underlyingType);
            
            switch (underlyingValue)
            {
                case byte b:
                    _byteFormatter!.Serialize(ref writer, b);
                    break;
                case ushort us:
                    _ushortFormatter!.Serialize(ref writer, us);
                    break;
                case uint ui:
                    _uintFormatter!.Serialize(ref writer, ui);
                    break;
                case ulong ul:
                    _ulongFormatter!.Serialize(ref writer, ul);
                    break;
                case sbyte sb:
                    _sbyteFormatter!.Serialize(ref writer, sb);
                    break;
                case short s:
                    _shortFormatter!.Serialize(ref writer, s);
                    break;
                case int i:
                    _intFormatter!.Serialize(ref writer, i);
                    break;
                case long l:
                    _longFormatter!.Serialize(ref writer, l);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported underlying type: {_underlyingType.Name}");
            }
        }

        public T Deserialize(ref BcsReader reader)
        {
            // Deserialize the underlying value and convert back to enum
            object underlyingValue;
            
            if (_underlyingType == typeof(byte))
                underlyingValue = _byteFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(ushort))
                underlyingValue = _ushortFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(uint))
                underlyingValue = _uintFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(ulong))
                underlyingValue = _ulongFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(sbyte))
                underlyingValue = _sbyteFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(short))
                underlyingValue = _shortFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(int))
                underlyingValue = _intFormatter!.Deserialize(ref reader);
            else if (_underlyingType == typeof(long))
                underlyingValue = _longFormatter!.Deserialize(ref reader);
            else
                throw new InvalidOperationException($"Unsupported underlying type: {_underlyingType.Name}");

            return (T)Enum.ToObject(typeof(T), underlyingValue);
        }

        public int? GetSerializedSize(T value)
        {
            // Size is determined by the underlying type
            if (_underlyingType == typeof(byte) || _underlyingType == typeof(sbyte))
                return 1;
            if (_underlyingType == typeof(ushort) || _underlyingType == typeof(short))
                return 2;
            if (_underlyingType == typeof(uint) || _underlyingType == typeof(int))
                return 4;
            if (_underlyingType == typeof(ulong) || _underlyingType == typeof(long))
                return 8;
                
            return null;
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