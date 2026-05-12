namespace BcsSharp.Core.Attributes;

/// <summary>
/// Declares the <see cref="IBcsFormatter{T}"/> implementation to use for the annotated type.
/// Lets a downstream assembly attach a custom formatter (e.g. a <c>ByteArrayFormatter&lt;T&gt;</c>
/// subclass) without modifying BcsSharp.Core's resolver chain.
/// </summary>
/// <remarks>
/// <para>
/// The formatter type is instantiated via <c>FormatterInstanceFactory</c>: a public static
/// <c>Instance</c> field/property is reused if present, otherwise a parameterless constructor
/// is invoked.
/// </para>
/// <para>
/// Open generic formatters are supported: applying
/// <c>[BcsFormatter(typeof(MyFormatter&lt;&gt;))]</c> on <c>MyType&lt;T&gt;</c> closes the
/// formatter with the same type arguments as the annotated type at resolve time.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum,
                Inherited = false, AllowMultiple = false)]
public sealed class BcsFormatterAttribute : Attribute
{
    public Type FormatterType { get; }

    public BcsFormatterAttribute(Type formatterType)
    {
        FormatterType = formatterType;
    }
}
