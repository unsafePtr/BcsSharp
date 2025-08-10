using System.Collections.Concurrent;

namespace BcsSharp.Core.Resolvers
{
    /// <summary>
    /// Composite resolver that combines multiple resolvers in priority order
    /// </summary>
    public sealed class CompositeResolver : IFormatterResolver
    {
        private readonly IFormatterResolver[] _resolvers;
        private static readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

        public CompositeResolver(params IFormatterResolver[] resolvers)
        {
            _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        }

        public IBcsFormatter<T>? GetFormatter<T>()
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), type =>
            {
                foreach (var resolver in _resolvers)
                {
                    var formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                        return formatter;
                }
                return null;
            });
        }

        /// <summary>
        /// Default instance with recommended resolver chain for nullable types, OneOf types, enums, objects, and standard types
        /// </summary>
        public static readonly CompositeResolver Default = new CompositeResolver(
            NullableResolver.Instance,
            OneOfResolver.Instance,
            NullableReferenceResolver.Instance,
            EnumResolver.Instance,
            ObjectResolver.Instance,
            StandardResolver.Instance
        );
    }
}