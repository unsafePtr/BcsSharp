using System;
using System.Collections.Concurrent;
using System.Reflection;
using BcsSharp.Core.Formatters;
using OneOf;
using OneOf.Types;

namespace BcsSharp.Core.Resolvers
{
    /// <summary>
    /// Resolver for OneOf&lt;None, T&gt; types that represent Rust Option&lt;T&gt;
    /// </summary>
    public sealed class OneOfResolver : IFormatterResolver
    {
        public static readonly OneOfResolver Instance = new();
        private static readonly ConcurrentDictionary<Type, object?> _formatterCache = new();

        private OneOfResolver() { }

        public IBcsFormatter<T>? GetFormatter<T>()
        {
            return (IBcsFormatter<T>?)_formatterCache.GetOrAdd(typeof(T), CreateFormatter);
        }

        private static object? CreateFormatter(Type type)
        {
            // Check if this is OneOf<None, T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OneOf<,>))
            {
                var genericArgs = type.GetGenericArguments();
                
                // Must be OneOf<None, T> where first type is None
                if (genericArgs.Length == 2 && genericArgs[0] == typeof(None))
                {
                    var valueType = genericArgs[1]; // The T in OneOf<None, T>
                    
                    // Get formatter for the value type
                    var valueFormatterMethod = typeof(BcsSerializer).GetMethod(nameof(BcsSerializer.GetFormatter), BindingFlags.Public | BindingFlags.Static);
                    var genericValueFormatterMethod = valueFormatterMethod?.MakeGenericMethod(valueType);
                    var valueFormatter = genericValueFormatterMethod?.Invoke(null, new object?[] { null });
                    
                    if (valueFormatter != null)
                    {
                        // Create OneOfFormatter<T>
                        var oneOfFormatterType = typeof(OneOfFormatter<>).MakeGenericType(valueType);
                        return Activator.CreateInstance(oneOfFormatterType, valueFormatter);
                    }
                }
            }

            return null;
        }
    }
}