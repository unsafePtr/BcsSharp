namespace BcsSharp.Core.Unions;

/// <summary>
/// The "none" case for <see cref="Option{T}"/>.
/// The constructor is private, so <see cref="Instance"/> is the one instance: absence costs a reference, and a deserialized None is reference-equal to it.
/// Deliberately not a record — the generated copy constructor lets <c>with { }</c> clone the singleton from outside, and a type with no state has nothing to copy or compare structurally.
/// </summary>
public sealed class None
{
    /// <summary>Shared singleton; assign this to indicate absence.</summary>
    public static readonly None Instance = new();

    private None() { }
}

/// <summary>
/// Reference-typed Option, modeled as a C# 15 union with no payload wrapper.
/// Construction is allocation-free: assigning either <see cref="None.Instance"/> or a non-null <typeparamref name="T"/> just stores a reference in the union's <c>Value</c> slot.
/// For value-typed optionality use <c>T?</c> (Nullable) instead — boxing T into this union would cost an allocation per construction.
/// </summary>
/// <typeparam name="T">A reference type.</typeparam>
public readonly union Option<T>(None, T) where T : class;
