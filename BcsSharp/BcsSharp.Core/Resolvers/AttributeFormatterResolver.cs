using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Resolvers;

/// <summary>
/// Resolves formatters declared via <see cref="BcsFormatterAttribute"/> on the target type.
/// Handles open-generic formatters by closing them with the resolved type's generic arguments.
/// </summary>
public sealed class AttributeFormatterResolver : IFormatterResolver
{
    public static readonly AttributeFormatterResolver Instance = new();

    private AttributeFormatterResolver() { }

    public IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root) => Cache<T>.Formatter;

    private static class Cache<T>
    {
        public static readonly IBcsFormatter<T>? Formatter = Resolve();

        private static IBcsFormatter<T>? Resolve()
        {
            var attr = typeof(T).GetCustomAttribute<BcsFormatterAttribute>(inherit: false);
            if (attr is null)
            {
                return null;
            }

            var formatterType = attr.FormatterType;
            if (formatterType.IsGenericTypeDefinition)
            {
                if (!typeof(T).IsGenericType)
                {
                    throw new InvalidOperationException(
                        $"[BcsFormatter] on {typeof(T).FullName} references the open generic " +
                        $"{formatterType.FullName} but {typeof(T).Name} is not generic.");
                }

                formatterType = formatterType.MakeGenericType(typeof(T).GetGenericArguments());
            }

            var instance = FormatterInstanceFactory.Create(formatterType);
            if (instance is not IBcsFormatter<T> typed)
            {
                throw new InvalidOperationException(
                    $"Formatter {formatterType.FullName} declared via [BcsFormatter] on " +
                    $"{typeof(T).FullName} does not implement IBcsFormatter<{typeof(T).Name}>.");
            }

            return typed;
        }
    }
}
