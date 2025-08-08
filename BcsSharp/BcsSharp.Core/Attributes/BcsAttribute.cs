using System;

namespace BcsSharp.Core.Attributes
{
    /// <summary>
    /// Marks a class for BCS serialization - used by source generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class BcsSerializableAttribute : Attribute
    {
        /// <summary>
        /// Custom type name for BCS serialization
        /// </summary>
        public string? TypeName { get; set; }

        public BcsSerializableAttribute() { }

        public BcsSerializableAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }

    /// <summary>
    /// Marks a property to be included/excluded from BCS serialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BcsPropertyAttribute : Attribute
    {
        /// <summary>
        /// Custom field name for BCS serialization
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Order of the property in serialization (default: declaration order)
        /// </summary>
        public int Order { get; set; } = -1;

        public BcsPropertyAttribute() { }

        public BcsPropertyAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Excludes a property from BCS serialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BcsIgnoreAttribute : Attribute
    {
    }
}