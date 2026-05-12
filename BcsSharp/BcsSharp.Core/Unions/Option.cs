namespace BcsSharp.Core.Unions;

/// <summary>
/// The "none" case for <see cref="Option{T}"/>. Use <see cref="Instance"/> to avoid
/// allocating a fresh instance per absence.
/// </summary>
public sealed record class None
{
    /// <summary>Shared singleton; assign this to indicate absence.</summary>
    public static readonly None Instance = new();
}

/// <summary>
/// Reference-typed Option, modeled as a C# 15 union with no payload wrapper.
/// Construction is allocation-free: assigning either <see cref="None.Instance"/>
/// or a non-null <typeparamref name="T"/> just stores a reference in the union's
/// <c>Value</c> slot. For value-typed optionality use <c>T?</c> (Nullable) instead —
/// boxing T into this union would cost an allocation per construction.
/// </summary>
/// <typeparam name="T">A reference type.</typeparam>
public union Option<T>(None, T) where T : class;
