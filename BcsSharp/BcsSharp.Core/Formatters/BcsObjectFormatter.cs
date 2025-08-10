using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// Formatter for BCS-serializable objects (structs/classes) marked with [BcsStruct].
    /// Serializes fields/properties in lexicographic order by name, unless explicit ordering is specified.
    /// 
    /// BCS Format: Fields are serialized consecutively in order without any headers or separators.
    /// Example: struct Person { name: String, age: u32 } -> [name_data...] + [age_data...]
    /// </summary>
    /// <typeparam name="T">The object type to serialize.</typeparam>
    public sealed class BcsObjectFormatter<T> : IBcsFormatter<T>, IBcsFormatter
    {
        private readonly List<BcsFieldInfo> _fields;
        private readonly Type _objectType;

        public Type TargetType => typeof(T);

        public BcsObjectFormatter()
        {
            _objectType = typeof(T);
            
            var bcsStructAttr = _objectType.GetCustomAttribute<BcsStructAttribute>();
            if (bcsStructAttr == null)
                throw new InvalidOperationException($"Type {_objectType.Name} must be marked with [BcsStruct] attribute");

            _fields = DiscoverFields(_objectType);
            
            if (!_fields.Any())
                throw new InvalidOperationException($"Type {_objectType.Name} has no serializable fields marked with [BcsField]");
        }

        public void Serialize(ref BcsWriter writer, T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            foreach (var field in _fields)
            {
                var fieldValue = field.GetValue(value);
                field.Formatter.SerializeObject(ref writer, fieldValue);
            }
        }

        public T Deserialize(ref BcsReader reader)
        {
            // Handle value types and reference types differently
            if (_objectType.IsValueType)
            {
                // For value types, we need to use boxing to properly set field values
                object boxedInstance = Activator.CreateInstance(_objectType)!;
                
                foreach (var field in _fields)
                {
                    var fieldValue = field.Formatter.DeserializeObject(ref reader);
                    field.SetValue(boxedInstance, fieldValue);
                }
                
                return (T)boxedInstance;
            }
            else
            {
                var instance = (T)Activator.CreateInstance(_objectType)!;
                
                foreach (var field in _fields)
                {
                    var fieldValue = field.Formatter.DeserializeObject(ref reader);
                    field.SetValue(instance, fieldValue);
                }
                
                return instance;
            }
        }

        public int? GetSerializedSize(T value)
        {
            if (value == null)
                return null;

            var totalSize = 0;
            
            foreach (var field in _fields)
            {
                var fieldValue = field.GetValue(value);
                var fieldSize = field.Formatter.GetObjectSize(fieldValue);
                
                if (fieldSize == null)
                    return null; // Variable size
                    
                totalSize += fieldSize.Value;
            }
            
            return totalSize;
        }

        private List<BcsFieldInfo> DiscoverFields(Type objectType)
        {
            var fields = new List<BcsFieldInfo>();
            
            // Get all fields and properties
            var members = new List<MemberInfo>();
            members.AddRange(objectType.GetFields(BindingFlags.Public | BindingFlags.Instance));
            members.AddRange(objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            
            foreach (var member in members)
            {
                var fieldAttr = member.GetCustomAttribute<BcsFieldAttribute>();
                if (fieldAttr == null)
                    continue;

                Type memberType;
                Func<object, object?> getter;
                Action<object, object?> setter;

                if (member is FieldInfo field)
                {
                    memberType = field.FieldType;
                    getter = obj => field.GetValue(obj);
                    setter = (obj, value) => field.SetValue(obj, value);
                }
                else if (member is PropertyInfo property)
                {
                    memberType = property.PropertyType;
                    
                    if (!property.CanRead)
                        throw new InvalidOperationException($"Property {member.Name} in {objectType.Name} must be readable");
                    if (!property.CanWrite)
                        throw new InvalidOperationException($"Property {member.Name} in {objectType.Name} must be writable");
                        
                    getter = obj => property.GetValue(obj);
                    setter = (obj, value) => property.SetValue(obj, value);
                }
                else
                {
                    continue; // Should never happen
                }

                var formatter = BcsSerializerExtensions.GetFormatter(memberType);
                if (formatter == null)
                    throw new InvalidOperationException($"No BCS formatter found for field {member.Name} of type {memberType.Name}");

                var fieldName = fieldAttr.Name ?? member.Name;
                var order = fieldAttr.Order ?? int.MaxValue; // Fields without explicit order go to the end

                fields.Add(new BcsFieldInfo
                {
                    Name = fieldName,
                    MemberName = member.Name,
                    Order = order,
                    MemberType = memberType,
                    Formatter = formatter,
                    GetValue = getter,
                    SetValue = setter
                });
            }

            // Sort fields: first by explicit order, then lexicographically by name
            return fields
                .OrderBy(f => f.Order != int.MaxValue ? 0 : 1) // Explicit order first
                .ThenBy(f => f.Order)
                .ThenBy(f => f.Name, StringComparer.Ordinal) // Lexicographic order for fields without explicit order
                .ToList();
        }

        private class BcsFieldInfo
        {
            public string Name { get; set; } = "";
            public string MemberName { get; set; } = "";
            public int Order { get; set; }
            public Type MemberType { get; set; } = null!;
            public IBcsObjectFormatter Formatter { get; set; } = null!;
            public Func<object, object?> GetValue { get; set; } = null!;
            public Action<object, object?> SetValue { get; set; } = null!;
        }
    }
}