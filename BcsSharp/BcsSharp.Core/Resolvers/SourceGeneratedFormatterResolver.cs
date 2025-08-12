using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Core.Resolvers
{
    /// <summary>
    /// Resolver that automatically finds and uses source-generated formatters.
    /// This resolver searches for GeneratedAssemblyBcsResolverAttribute on assemblies
    /// to locate assembly-specific generated resolvers.
    /// </summary>
    public sealed class SourceGeneratedFormatterResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly SourceGeneratedFormatterResolver Instance = new();

        private static readonly ConcurrentDictionary<Assembly, IFormatterResolver?> AssemblyResolverCache = new();

        private SourceGeneratedFormatterResolver()
        {
        }

        /// <inheritdoc/>
        public IBcsFormatter<T>? GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            internal static readonly IBcsFormatter<T>? Formatter = FindPrecompiledFormatter();

            private static IBcsFormatter<T>? FindPrecompiledFormatter()
            {
                IFormatterResolver? resolver = AssemblyResolverCache.GetOrAdd(typeof(T).Assembly, static assembly =>
                {
                    if (typeof(T).Assembly.GetCustomAttributes<GeneratedAssemblyBcsResolverAttribute>().FirstOrDefault() is { } att)
                    {
                        return (IFormatterResolver?)att.ResolverType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    }

                    return null;
                });

                return resolver?.GetFormatter<T>();
            }
        }
    }
}