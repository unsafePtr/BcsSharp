using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// Non-generic interface for field operations
    /// </summary>
    public interface IBcsFieldAccessor<T>
    {
        string Name { get; }
        void WriteField(T obj, BcsWriter writer);
        void ReadField(T obj, BcsReader reader);
        int? GetFieldSize(T obj);
    }

    /// <summary>
    /// Generic field accessor that maintains full type safety
    /// </summary>
    public class BcsFieldAccessor<T, TField> : IBcsFieldAccessor<T>
    {
        public string Name { get; }
        private readonly BcsType<TField> _bcsType;
        private readonly Func<T, TField> _getter;
        private readonly Action<T, TField> _setter;

        public BcsFieldAccessor(string name, BcsType<TField> bcsType, 
            Func<T, TField> getter, Action<T, TField> setter)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _bcsType = bcsType ?? throw new ArgumentNullException(nameof(bcsType));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public void WriteField(T obj, BcsWriter writer)
        {
            var fieldValue = _getter(obj);
            _bcsType.Write(fieldValue, writer);
        }

        public void ReadField(T obj, BcsReader reader)
        {
            var fieldValue = _bcsType.Read(reader);
            _setter(obj, fieldValue);
        }

        public int? GetFieldSize(T obj)
        {
            var fieldValue = _getter(obj);
            return _bcsType.SerializedSize(fieldValue);
        }
    }

    /// <summary>
    /// BCS struct type that can serialize/deserialize objects with multiple fields
    /// </summary>
    public class StructType<T> : BcsType<T> where T : class, new()
    {
        private readonly List<IBcsFieldAccessor<T>> _fieldAccessors;

        public StructType(string name, List<IBcsFieldAccessor<T>> fieldAccessors) : base(name)
        {
            _fieldAccessors = fieldAccessors ?? throw new ArgumentNullException(nameof(fieldAccessors));
        }

        public override T Read(BcsReader reader)
        {
            var result = new T();

            foreach (var fieldAccessor in _fieldAccessors)
            {
                fieldAccessor.ReadField(result, reader);
            }

            return result;
        }

        public override void Write(T value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            foreach (var fieldAccessor in _fieldAccessors)
            {
                fieldAccessor.WriteField(value, writer);
            }
        }

        public override int? SerializedSize(T value)
        {
            if (value == null) return null;

            int totalSize = 0;

            foreach (var fieldAccessor in _fieldAccessors)
            {
                var fieldSize = fieldAccessor.GetFieldSize(value);
                if (fieldSize == null)
                    return null;

                totalSize += fieldSize.Value;
            }

            return totalSize;
        }
    }

    /// <summary>
    /// Builder for creating struct types with fluent API
    /// </summary>
    public class StructTypeBuilder<T> where T : class, new()
    {
        private readonly string _name;
        private readonly List<IBcsFieldAccessor<T>> _fieldAccessors = new();

        public StructTypeBuilder(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Add a field to the struct - fully generic, no object usage
        /// </summary>
        public StructTypeBuilder<T> AddField<TField>(string name, BcsType<TField> bcsType,
            Func<T, TField> getter, Action<T, TField> setter)
        {
            var fieldAccessor = new BcsFieldAccessor<T, TField>(name, bcsType, getter, setter);
            _fieldAccessors.Add(fieldAccessor);
            return this;
        }

        /// <summary>
        /// Add a field using property expression - automatically generates getter/setter
        /// </summary>
        public StructTypeBuilder<T> AddField<TField>(BcsType<TField> bcsType, Expression<Func<T, TField>> propertyExpression)
        {
            var (propertyName, getter, setter) = ExtractPropertyInfo(propertyExpression);
            var fieldAccessor = new BcsFieldAccessor<T, TField>(propertyName, bcsType, getter, setter);
            _fieldAccessors.Add(fieldAccessor);
            return this;
        }

        /// <summary>
        /// Add a field with custom name using property expression
        /// </summary>
        public StructTypeBuilder<T> AddField<TField>(string name, BcsType<TField> bcsType, Expression<Func<T, TField>> propertyExpression)
        {
            var (_, getter, setter) = ExtractPropertyInfo(propertyExpression);
            var fieldAccessor = new BcsFieldAccessor<T, TField>(name, bcsType, getter, setter);
            _fieldAccessors.Add(fieldAccessor);
            return this;
        }

        /// <summary>
        /// Extract property information from expression
        /// </summary>
        private static (string PropertyName, Func<T, TField> Getter, Action<T, TField> Setter) ExtractPropertyInfo<TField>(
            Expression<Func<T, TField>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression memberExpression)
                throw new ArgumentException("Expression must be a property access", nameof(propertyExpression));

            if (memberExpression.Member is not PropertyInfo propertyInfo)
                throw new ArgumentException("Expression must access a property", nameof(propertyExpression));

            var propertyName = propertyInfo.Name;

            // Create compiled getter
            var getter = propertyExpression.Compile();

            // Create setter using expression trees
            var instanceParam = Expression.Parameter(typeof(T), "instance");
            var valueParam = Expression.Parameter(typeof(TField), "value");
            var propertyAccess = Expression.Property(instanceParam, propertyInfo);
            var assign = Expression.Assign(propertyAccess, valueParam);
            var setterExpression = Expression.Lambda<Action<T, TField>>(assign, instanceParam, valueParam);
            var setter = setterExpression.Compile();

            return (propertyName, getter, setter);
        }

        /// <summary>
        /// Build the struct type
        /// </summary>
        public StructType<T> Build()
        {
            return new StructType<T>(_name, _fieldAccessors);
        }
    }

    /// <summary>
    /// Static helper methods for creating struct types
    /// </summary>
    public static class BcsStruct
    {
        /// <summary>
        /// Create auto-discovery struct type (default) - uses class name
        /// </summary>
        public static AutoStructType<T> Create<T>() where T : class, new()
        {
            return new AutoStructType<T>();
        }

        /// <summary>
        /// Create auto-discovery struct type - custom name
        /// </summary>
        public static AutoStructType<T> Create<T>(string name) where T : class, new()
        {
            return new AutoStructType<T>(name);
        }

        /// <summary>
        /// Create manual struct type builder for advanced scenarios
        /// </summary>
        public static StructTypeBuilder<T> CreateManual<T>(string name) where T : class, new()
        {
            return new StructTypeBuilder<T>(name);
        }
    }
}