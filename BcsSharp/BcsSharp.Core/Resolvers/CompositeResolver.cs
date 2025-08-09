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
        /// Default instance with recommended resolver chain for nullable types, enums, and standard types
        /// </summary>
        public static readonly CompositeResolver Default = new CompositeResolver(
            NullableResolver.Instance,
            NullableReferenceResolver.Instance,
            EnumResolver.Instance,
            StandardResolver.Instance
        );
        
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
        
        /// <summary>
        /// Create a resolver with the default chain plus additional custom resolvers
        /// </summary>
        public static CompositeResolver CreateWithDefaults(params IFormatterResolver[] customResolvers)
        {
            var defaultResolvers = new IFormatterResolver[] { NullableResolver.Instance, NullableReferenceResolver.Instance, EnumResolver.Instance, StandardResolver.Instance };
            var allResolvers = new IFormatterResolver[defaultResolvers.Length + customResolvers.Length];
            Array.Copy(defaultResolvers, 0, allResolvers, 0, defaultResolvers.Length);
            Array.Copy(customResolvers, 0, allResolvers, defaultResolvers.Length, customResolvers.Length);
            return new CompositeResolver(allResolvers);
        }
    }
}