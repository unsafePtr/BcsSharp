using System;
using System.Collections.Generic;
using System.Linq;

namespace BcsSharp.Core
{
    /// <summary>
    /// Represents an enum variant with optional associated data
    /// </summary>
    public class EnumVariant
    {
        public string Name { get; }
        public object? Data { get; }
        public Type? DataType { get; }

        public EnumVariant(string name, object? data = null, Type? dataType = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Data = data;
            DataType = dataType;
        }

        public bool HasData => Data != null;

        public T GetData<T>() => (T)Data!;

        public override string ToString() => HasData ? $"{Name}({Data})" : Name;

        public override bool Equals(object? obj)
        {
            if (obj is not EnumVariant other) return false;
            return Name == other.Name && Equals(Data, other.Data);
        }

        public override int GetHashCode() => HashCode.Combine(Name, Data);
    }

    /// <summary>
    /// Definition of an enum variant for BCS serialization
    /// </summary>
    public class EnumVariantDefinition
    {
        public string Name { get; }
        public object? BcsType { get; }
        public Type? DataType { get; }

        public EnumVariantDefinition(string name, object? bcsType = null, Type? dataType = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BcsType = bcsType;
            DataType = dataType;
        }

        public bool HasData => BcsType != null;
    }

    /// <summary>
    /// BCS enum type that can serialize/deserialize tagged union types
    /// </summary>
    public class EnumType : BcsType<EnumVariant>
    {
        private readonly List<EnumVariantDefinition> _variants;
        private readonly Dictionary<string, int> _variantIndexes;

        public EnumType(string name, List<EnumVariantDefinition> variants) : base(name)
        {
            _variants = variants ?? throw new ArgumentNullException(nameof(variants));
            _variantIndexes = variants.Select((v, i) => new { v.Name, Index = i })
                                    .ToDictionary(x => x.Name, x => x.Index);
        }

        public override EnumVariant Read(BcsReader reader)
        {
            var index = (int)reader.ReadULEB32();
            
            if (index >= _variants.Count)
                throw new InvalidOperationException($"Unknown enum variant index {index} for enum {Name}");
            
            var variant = _variants[index];
            
            if (variant.HasData)
            {
                // Use reflection to call Read method on the variant's BcsType
                var readMethod = variant.BcsType!.GetType().GetMethod("Read");
                if (readMethod == null)
                    throw new InvalidOperationException($"No Read method found for variant {variant.Name}");
                
                // Since BcsReader is a ref struct, we need special handling for reflection
                var parameters = new object[] { reader };
                var data = readMethod.Invoke(variant.BcsType, parameters);
                reader = (BcsReader)parameters[0]; // Update reader position
                return new EnumVariant(variant.Name, data, variant.DataType);
            }
            else
            {
                return new EnumVariant(variant.Name);
            }
        }

        public override void Write(EnumVariant value, BcsWriter writer)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            if (!_variantIndexes.TryGetValue(value.Name, out var index))
                throw new ArgumentException($"Unknown enum variant: {value.Name}");
            
            writer.WriteULEB((uint)index);
            
            var variant = _variants[index];
            if (variant.HasData)
            {
                if (!value.HasData)
                    throw new ArgumentException($"Variant {value.Name} requires data but none provided");
                
                // Use reflection to call Write method on the variant's BcsType
                var writeMethod = variant.BcsType!.GetType().GetMethod("Write");
                if (writeMethod == null)
                    throw new InvalidOperationException($"No Write method found for variant {variant.Name}");
                
                // Since BcsWriter is a ref struct, we need special handling for reflection
                var parameters = new object[] { value.Data!, writer };
                writeMethod.Invoke(variant.BcsType, parameters);
                writer = (BcsWriter)parameters[1]; // Update writer state
            }
            else if (value.HasData)
            {
                throw new ArgumentException($"Variant {value.Name} does not accept data but data was provided");
            }
        }

        public override int? SerializedSize(EnumVariant value)
        {
            if (value == null) return null;
            
            if (!_variantIndexes.TryGetValue(value.Name, out var index))
                return null;
            
            // Size of ULEB variant index (simplified - assuming single byte for small enums)
            int totalSize = 1;
            
            var variant = _variants[index];
            if (variant.HasData)
            {
                if (!value.HasData) return null;
                
                // Use reflection to call SerializedSize method on the variant's BcsType
                var sizeMethod = variant.BcsType!.GetType().GetMethod("SerializedSize");
                if (sizeMethod == null) return null;
                
                var dataSize = sizeMethod.Invoke(variant.BcsType, new object[] { value.Data! });
                if (dataSize == null) return null;
                
                totalSize += (int)dataSize;
            }
            
            return totalSize;
        }

        /// <summary>
        /// Create an enum variant without data
        /// </summary>
        public EnumVariant CreateVariant(string name)
        {
            if (!_variantIndexes.ContainsKey(name))
                throw new ArgumentException($"Unknown variant: {name}");
            
            var variant = _variants[_variantIndexes[name]];
            if (variant.HasData)
                throw new ArgumentException($"Variant {name} requires data");
            
            return new EnumVariant(name);
        }

        /// <summary>
        /// Create an enum variant with data
        /// </summary>
        public EnumVariant CreateVariant<T>(string name, T data)
        {
            if (!_variantIndexes.ContainsKey(name))
                throw new ArgumentException($"Unknown variant: {name}");
            
            var variant = _variants[_variantIndexes[name]];
            if (!variant.HasData)
                throw new ArgumentException($"Variant {name} does not accept data");
            
            return new EnumVariant(name, data, typeof(T));
        }
    }

    /// <summary>
    /// Builder for creating enum types with fluent API
    /// </summary>
    public class EnumTypeBuilder
    {
        private readonly string _name;
        private readonly List<EnumVariantDefinition> _variants = new();

        public EnumTypeBuilder(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Add a variant without associated data
        /// </summary>
        public EnumTypeBuilder AddVariant(string name)
        {
            _variants.Add(new EnumVariantDefinition(name));
            return this;
        }

        /// <summary>
        /// Add a variant with associated data
        /// </summary>
        public EnumTypeBuilder AddVariant<T>(string name, BcsType<T> bcsType)
        {
            _variants.Add(new EnumVariantDefinition(name, bcsType, typeof(T)));
            return this;
        }

        /// <summary>
        /// Build the enum type
        /// </summary>
        public EnumType Build()
        {
            if (_variants.Count == 0)
                throw new InvalidOperationException("Enum must have at least one variant");
            
            return new EnumType(_name, _variants);
        }
    }

    /// <summary>
    /// Static helper methods for creating enum types
    /// </summary>
    public static class BcsEnum
    {
        /// <summary>
        /// Create an enum type builder
        /// </summary>
        public static EnumTypeBuilder Create(string name)
        {
            return new EnumTypeBuilder(name);
        }
    }
}