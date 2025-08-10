using System;

namespace BcsSharp.Core.Attributes
{
    /// <summary>
    /// Marks a type as a BCS-serializable struct.
    /// BCS structs serialize their fields in lexicographic order by field name,
    /// unless explicit ordering is specified via BcsFieldAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class BcsStructAttribute : Attribute
    {
        // No additional properties needed - BCS structs follow standard field ordering
    }

    /// <summary>
    /// Marks a field or property for BCS serialization within a struct.
    /// Fields are serialized in lexicographic order unless explicit order is specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BcsFieldAttribute : Attribute
    {
        /// <summary>
        /// Gets the explicit order of this field in serialization.
        /// If not specified, fields are ordered lexicographically by name.
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// Gets or sets the field name to use in serialization.
        /// If not specified, uses the actual field/property name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Initializes a new instance with automatic ordering (lexicographic by name).
        /// </summary>
        public BcsFieldAttribute()
        {
            Order = null;
        }

        /// <summary>
        /// Initializes a new instance with explicit field order.
        /// </summary>
        /// <param name="order">The 0-based index of this field in serialization order.</param>
        public BcsFieldAttribute(int order)
        {
            Order = order;
        }
    }
}