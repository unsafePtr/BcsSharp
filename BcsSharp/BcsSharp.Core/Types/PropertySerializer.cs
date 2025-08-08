using System;
using System.Reflection;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// Base class for property serializers - moved outside AutoStructType to avoid reflection issues
    /// </summary>
    public abstract class PropertySerializer
    {
        public string PropertyName { get; }

        protected PropertySerializer(string propertyName)
        {
            PropertyName = propertyName;
        }

        public abstract void Read(object instance, BcsReader reader);
        public abstract void Write(object instance, BcsWriter writer);
        public abstract int? GetSize(object instance);
    }

    /// <summary>
    /// Generic property serializer using reflection - replaces nested implementation
    /// </summary>
    public class GenericPropertySerializer : PropertySerializer
    {
        private readonly PropertyInfo _property;
        private readonly object _bcsTypeInstance;

        public GenericPropertySerializer(PropertyInfo property, object bcsTypeInstance)
            : base(property.Name)
        {
            _property = property;
            _bcsTypeInstance = bcsTypeInstance;
        }

        public override void Read(object instance, BcsReader reader)
        {
            // Use reflection to call Read method on the BCS type
            var readMethod = _bcsTypeInstance.GetType().GetMethod("Read");
            if (readMethod == null) return;

            var value = readMethod.Invoke(_bcsTypeInstance, new object[] { reader });
            _property.SetValue(instance, value);
        }

        public override void Write(object instance, BcsWriter writer)
        {
            var value = _property.GetValue(instance);
            if (value == null) return;

            // Use reflection to call Write method on the BCS type
            var writeMethod = _bcsTypeInstance.GetType().GetMethod("Write");
            if (writeMethod == null) return;

            writeMethod.Invoke(_bcsTypeInstance, new object[] { value, writer });
        }

        public override int? GetSize(object instance)
        {
            var value = _property.GetValue(instance);
            if (value == null) return null;

            // Use reflection to call SerializedSize method on the BCS type
            var sizeMethod = _bcsTypeInstance.GetType().GetMethod("SerializedSize");
            if (sizeMethod == null) return null;

            var result = sizeMethod.Invoke(_bcsTypeInstance, new object[] { value });
            return result as int?;
        }
    }

    /// <summary>
    /// Typed property serializer for basic types - avoids reflection
    /// </summary>
    public class TypedPropertySerializer<TProperty> : PropertySerializer
    {
        private readonly PropertyInfo _property;
        private readonly BcsType<TProperty> _bcsType;

        public TypedPropertySerializer(PropertyInfo property, BcsType<TProperty> bcsType)
            : base(property.Name)
        {
            _property = property;
            _bcsType = bcsType;
        }

        public override void Read(object instance, BcsReader reader)
        {
            var value = _bcsType.Read(reader);
            _property.SetValue(instance, value);
        }

        public override void Write(object instance, BcsWriter writer)
        {
            var value = (TProperty)_property.GetValue(instance)!;
            _bcsType.Write(value, writer);
        }

        public override int? GetSize(object instance)
        {
            var value = (TProperty)_property.GetValue(instance)!;
            return _bcsType.SerializedSize(value);
        }
    }

    /// <summary>
    /// Specialized property serializer for C# enums - converts to/from byte values per BCS standard
    /// </summary>
    public class EnumPropertySerializer : PropertySerializer
    {
        private readonly PropertyInfo _property;
        private readonly Type _enumType;

        public EnumPropertySerializer(PropertyInfo property, Type enumType)
            : base(property.Name)
        {
            _property = property;
            _enumType = enumType;
        }

        public override void Read(object instance, BcsReader reader)
        {
            var byteValue = reader.Read8();
            var enumValue = Enum.ToObject(_enumType, byteValue);
            _property.SetValue(instance, enumValue);
        }

        public override void Write(object instance, BcsWriter writer)
        {
            var enumValue = _property.GetValue(instance)!;
            var byteValue = Convert.ToByte(enumValue);
            writer.Write8(byteValue);
        }

        public override int? GetSize(object instance)
        {
            return 1; // Enums are always serialized as single bytes in BCS
        }
    }
}