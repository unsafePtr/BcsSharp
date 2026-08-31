namespace BcsSharp.Core;

/// <summary>
/// Formatter interface for BCS serialization, similar to MessagePack's IMessagePackFormatter
/// </summary>
public interface IBcsFormatter<T>
{
    /// <summary>
    /// Serialize value to BcsWriter
    /// </summary>
    void Serialize(ref BcsWriter writer, T value);

    /// <summary>
    /// Deserialize value from BcsReader (allocating).
    /// Always produces a fresh instance.
    /// </summary>
    T Deserialize(ref BcsReader reader);

    /// <summary>
    /// In-place deserialize.
    /// Class targets reuse the existing instance when non-null, collections are cleared and refilled, value-type fields are written directly into the caller's storage.
    /// Default delegates to the allocating overload; formatters override to save allocations on hot read paths.
    /// </summary>
    void Deserialize(ref BcsReader reader, ref T value)
    {
        value = Deserialize(ref reader);
    }
}
