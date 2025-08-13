using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;

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
            private static readonly Dictionary<Type, object> WellKnownFormatters = new()
            {
                    { typeof(string), StringFormatter.Instance },

                    { typeof(byte), ByteFormatter.Instance },
                    { typeof(sbyte), SByteFormatter.Instance },
                    { typeof(ushort), UInt16Formatter.Instance },
                    { typeof(short), Int16Formatter.Instance },
                    { typeof(uint), UInt32Formatter.Instance },
                    { typeof(int), Int32Formatter.Instance },
                    { typeof(ulong), UInt64Formatter.Instance },
                    { typeof(long), Int64Formatter.Instance },
                    { typeof(UInt128), UInt128Formatter.Instance },
                    { typeof(Int128), Int128Formatter.Instance },
                    { typeof(Nethermind.Int256.UInt256), UInt256Formatter.Instance },
                    { typeof(bool), BoolFormatter.Instance },

                    { typeof(byte?), OptionFormatterCache.ByteOptionFormatter },
                    { typeof(sbyte?), OptionFormatterCache.SByteOptionFormatter },
                    { typeof(ushort?), OptionFormatterCache.UInt16OptionFormatter },
                    { typeof(short?), OptionFormatterCache.Int16OptionFormatter },
                    { typeof(uint?), OptionFormatterCache.UInt32OptionFormatter },
                    { typeof(int?), OptionFormatterCache.Int32OptionFormatter },
                    { typeof(ulong?), OptionFormatterCache.UInt64OptionFormatter },
                    { typeof(long?), OptionFormatterCache.Int64OptionFormatter },
                    { typeof(UInt128?), OptionFormatterCache.UInt128OptionFormatter },
                    { typeof(Int128?), OptionFormatterCache.Int128OptionFormatter },
                    { typeof(Nethermind.Int256.UInt256?), OptionFormatterCache.UInt256OptionFormatter },
                    { typeof(bool?), OptionFormatterCache.BoolOptionFormatter }

            };


            internal static readonly IBcsFormatter<T>? Formatter = FindPrecompiledFormatter();

            private static IBcsFormatter<T>? FindPrecompiledFormatter()
            {
                // First try the type's own assembly
                IFormatterResolver? resolver = AssemblyResolverCache.GetOrAdd(typeof(T).Assembly, static assembly =>
                {
                    if (typeof(T).Assembly.GetCustomAttributes<GeneratedAssemblyBcsResolverAttribute>().FirstOrDefault() is { } att)
                    {
                        return (IFormatterResolver?)att.ResolverType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    }

                    return null;
                });

                var formatter = resolver?.GetFormatter<T>();
                if (formatter != null)
                    return formatter;

                // If not found in type's assembly, search all loaded assemblies for Dictionary formatters
                // This is needed because Dictionary<K,V> is a system type but formatters are generated in user assemblies
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly == typeof(T).Assembly) continue; // Already checked above
                        
                        var assemblyResolver = AssemblyResolverCache.GetOrAdd(assembly, static asm =>
                        {
                            if (asm.GetCustomAttributes<GeneratedAssemblyBcsResolverAttribute>().FirstOrDefault() is { } att)
                            {
                                return (IFormatterResolver?)att.ResolverType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                            }
                            return null;
                        });

                        formatter = assemblyResolver?.GetFormatter<T>();
                        if (formatter != null)
                            return formatter;
                    }
                }

                // Fall back to well-known formatters
                return WellKnownFormatters.TryGetValue(typeof(T), out var f) ? f as IBcsFormatter<T> : null;
            }


        }
    }
}