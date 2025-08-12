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
        /// Default instance with recommended resolver chain for source-generated, nullable types, OneOf types, enums, objects, and standard types
        /// </summary>
        public static readonly CompositeResolver Default = new CompositeResolver(
            CreateResolverChain()
        );

        private static IFormatterResolver[] CreateResolverChain()
        {
            var resolvers = new List<IFormatterResolver>();

            // Try to add source generator resolver first (highest priority)
            try
            {
                // Look for the source generator resolver in any loaded assembly
                var sourceGenResolverType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(type => type.Name == "BcsSourceGeneratorResolver" && 
                                          type.Namespace == "BcsSharp.Generated" &&
                                          typeof(IFormatterResolver).IsAssignableFrom(type));
                
                if (sourceGenResolverType != null)
                {
                    // Try to get Instance field (source-generated resolvers use fields)
                    var instanceField = sourceGenResolverType.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var instance = instanceField?.GetValue(null);
                    
                    if (instance is IFormatterResolver sourceGenResolver)
                    {
                        resolvers.Add(sourceGenResolver);
                    }
                    else
                    {
                        // Fallback to property for backwards compatibility
                        var instanceProperty = sourceGenResolverType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        instance = instanceProperty?.GetValue(null);
                        
                        if (instance is IFormatterResolver fallbackResolver)
                        {
                            resolvers.Add(fallbackResolver);
                        }
                    }
                }
            }
            catch
            {
                // Source generator not available, continue with other resolvers
            }

            // Add standard resolvers
            resolvers.AddRange([
                StandardResolver.Instance,
                NullableResolver.Instance,
                OneOfResolver.Instance,
                EnumResolver.Instance,
                ObjectResolver.Instance
            ]);

            return resolvers.ToArray();
        }
    }
}