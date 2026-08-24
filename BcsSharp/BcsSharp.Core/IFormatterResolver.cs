using System.Runtime.CompilerServices;

namespace BcsSharp.Core;

/// <summary>
/// Resolver interface for finding formatters, similar to MessagePack's IFormatterResolver
/// </summary>
public interface IFormatterResolver
{
    /// <summary>
    /// Get formatter for specified type. <paramref name="root"/> is the chain that started
    /// resolution; composing resolvers must look their children up through it rather than
    /// through the global default, so a scoped chain stays scoped. Null means "this".
    /// </summary>
    IBcsFormatter<T>? GetFormatter<T>(IFormatterResolver? root);
}

public static class FormatterResolverExtensions
{
    /// <summary>
    /// Resolve through <paramref name="resolver"/> itself. Kept as an extension rather than
    /// an optional parameter so it stays a genuine zero-argument method — an optional
    /// parameter would break every method-group conversion of <c>GetFormatter</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IBcsFormatter<T>? GetFormatter<T>(this IFormatterResolver resolver) =>
        resolver.GetFormatter<T>(null);
}
