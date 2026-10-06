namespace BcsSharp.Core.Formatters;

/// <summary>
/// Stand-in handed out when a type's formatter is requested while that same formatter is still being built, which is what a recursive type does for itself.
/// It binds to the finished formatter from the chain's cache on first use, so the back edge of the recursion costs one extra virtual call and nothing is built twice.
/// </summary>
internal sealed class DeferredFormatter<T>(IFormatterResolver chain) : IBcsFormatter<T>
{
    private IBcsFormatter<T>? _target;

    private IBcsFormatter<T> Target => _target ??= chain.GetFormatter<T>()
        ?? throw new InvalidOperationException($"No formatter found for type {typeof(T)}");

    public void Serialize(ref BcsWriter writer, T value) => Target.Serialize(ref writer, value);

    public T Deserialize(ref BcsReader reader) => Target.Deserialize(ref reader);

    public void Deserialize(ref BcsReader reader, ref T value) => Target.Deserialize(ref reader, ref value);
}
