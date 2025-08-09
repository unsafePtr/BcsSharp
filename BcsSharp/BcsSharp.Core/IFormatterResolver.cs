namespace BcsSharp.Core
{
    /// <summary>
    /// Resolver interface for finding formatters, similar to MessagePack's IFormatterResolver
    /// </summary>
    public interface IFormatterResolver
    {
        /// <summary>
        /// Get formatter for specified type
        /// </summary>
        IBcsFormatter<T>? GetFormatter<T>();
    }
}