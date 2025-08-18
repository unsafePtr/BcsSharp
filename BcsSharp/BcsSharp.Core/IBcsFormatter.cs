namespace BcsSharp.Core
{
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
        /// Deserialize value from BcsReader
        /// </summary>
        T Deserialize(ref BcsReader reader);
        
    }
    
    /// <summary>
    /// Non-generic base interface for formatter discovery
    /// </summary>
    public interface IBcsFormatter
    {
        Type TargetType { get; }
    }
}