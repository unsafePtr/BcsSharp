using System;
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
        /// Create a resolver that first tries standard types, then custom resolvers
        /// </summary>
        public static CompositeResolver Create(params IFormatterResolver[] customResolvers)
        {
            var allResolvers = new IFormatterResolver[customResolvers.Length + 1];
            allResolvers[0] = StandardResolver.Instance;
            Array.Copy(customResolvers, 0, allResolvers, 1, customResolvers.Length);
            return new CompositeResolver(allResolvers);
        }
    }
}