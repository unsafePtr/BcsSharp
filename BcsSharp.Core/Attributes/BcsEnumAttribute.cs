namespace BcsSharp.Core.Attributes;

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
/// The class must be declared in the marker's assembly, nested in the marker or beside it; no other assembly is searched.
/// The index is required because inferring it from discovery order would tie the wire format to the order the compiler emits types in.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class BcsEnumVariantAttribute : Attribute
{
    /// <summary>
    /// The 0-based variant index written on the wire, unique within the enum.
    /// </summary>
    public uint Index { get; }

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
    /// If not specified, properties are serialized in declaration order, which reflection reports deterministically since .NET 7; inherited properties come after the variant's own.
    /// Give every data property of a variant an explicit order or none: an implicit order counts the properties before it, so mixing the two can give two properties the same order.
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
