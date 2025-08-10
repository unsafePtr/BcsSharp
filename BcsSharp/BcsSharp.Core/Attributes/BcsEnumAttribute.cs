using System;

namespace BcsSharp.Core.Attributes
{
    /// <summary>
    /// Marks a type as a BCS-serializable enum following Rust-style enum semantics.
    /// BCS enums use ULEB128-encoded variant indices and support associated data of any BCS type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class BcsEnumAttribute : Attribute
    {
        // No additional properties needed - BCS is a binary format,
        // only variant indices and data are serialized
    }

    /// <summary>
    /// Marks a class as a BCS enum variant.
    /// Each variant class represents one possible state of the enum with its associated data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class BcsEnumVariantAttribute : Attribute
    {
        /// <summary>
        /// Gets the variant index (0-based).
        /// If not specified, variants are indexed in the order they appear in the enum definition.
        /// </summary>
        public uint? Index { get; }

        /// <summary>
        /// Initializes a new instance with automatic index assignment.
        /// </summary>
        public BcsEnumVariantAttribute()
        {
            Index = null;
        }

        /// <summary>
        /// Initializes a new instance with explicit variant index.
        /// </summary>
        /// <param name="index">The 0-based variant index.</param>
        public BcsEnumVariantAttribute(uint index)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Marks a property as part of a BCS enum variant's associated data.
    /// The property value will be serialized after the variant index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BcsEnumDataAttribute : Attribute
    {
        /// <summary>
        /// Gets the order/index of this property in tuple-style variants.
        /// If not specified, properties are serialized in declaration order.
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// Initializes a new instance with automatic ordering.
        /// </summary>
        public BcsEnumDataAttribute()
        {
            Order = null;
        }

        /// <summary>
        /// Initializes a new instance with explicit field order.
        /// </summary>
        /// <param name="order">The 0-based index of this property in serialization order.</param>
        public BcsEnumDataAttribute(int order)
        {
            Order = order;
        }
    }
}