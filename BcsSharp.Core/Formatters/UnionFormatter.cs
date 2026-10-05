using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using BcsSharp.Core.Unions;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// BCS formatter for C# 15 <c>union</c> types.
/// Variant index = declaration order of the compiler-synthesised single-argument case constructors.
/// Each case payload is serialized via its own formatter, resolved through the standard chain.
///
/// Case-payload resolution rules:
/// <list type="bullet">
///   <item>Unit-like cases (no instance fields/properties — e.g. an empty record class like <c>None</c>) emit a zero-byte payload, matching Rust's unit-variant wire format.</item>
///   <item>Any other case type must be resolvable by the active formatter resolver (i.e. primitive, <c>[BcsStruct]</c>, or registered manually).</item>
/// </list>
/// </summary>
/// <remarks>
/// The Value getter is compiled as a delegate against the union struct's <c>Value</c> property directly, avoiding the 24 B/op boxing cost of casting the struct to <see cref="IUnion"/>.
/// Per-case read and write run through typed delegates closed over <see cref="IBcsFormatter{T}"/> rather than the <c>object?</c> boundary of <see cref="IBcsObjectFormatter"/>, so a struct payload is boxed once — by the union itself — instead of twice.
/// A unit case deserializes to one shared instance built at formatter construction: a zero-byte payload carries no state that could distinguish two instances, and reusing the singleton is what makes absence free.
/// </remarks>
public sealed class UnionFormatter<TUnion> : IBcsFormatter<TUnion>
{
    private delegate void CaseWriter(ref BcsWriter writer, object payload);

    private delegate TUnion CaseReader(ref BcsReader reader);

    private static readonly MethodInfo CreateCaseMethod =
        typeof(UnionFormatter<TUnion>).GetMethod(nameof(CreateCase), BindingFlags.NonPublic | BindingFlags.Static)!;

    // Rust's Option is not a container, so the Option<T> union must not count toward the depth limit.
    // Unions are structs, so each instantiation gets its own code and the JIT folds this into a constant.
    private static readonly bool CountsTowardDepth =
        !(typeof(TUnion).IsGenericType && typeof(TUnion).GetGenericTypeDefinition() == typeof(Option<>));

    private readonly UnionCase[] _cases;
    private readonly FrozenDictionary<Type, int> _caseIndexByType;
    private readonly Func<TUnion, object?> _valueGetter;

    public UnionFormatter() : this(null) { }

    public UnionFormatter(IFormatterResolver? root)
    {
        var unionType = typeof(TUnion);
        if (unionType.GetCustomAttribute<UnionAttribute>() is null)
        {
            throw new InvalidOperationException(
                $"Type {unionType.FullName} is not a C# 15 union (missing [Union] attribute).");
        }

        var ctors = unionType.GetConstructors()
            .Where(c => c.GetParameters().Length == 1)
            .OrderBy(c => c.MetadataToken)
            .ToArray();

        if (ctors.Length == 0)
        {
            throw new InvalidOperationException(
                $"Union {unionType.FullName} exposes no single-argument case constructors.");
        }

        _cases = new UnionCase[ctors.Length];
        var indexBuilder = new Dictionary<Type, int>(ctors.Length);
        for (int i = 0; i < ctors.Length; i++)
        {
            var caseType = ctors[i].GetParameters()[0].ParameterType;

            if (!indexBuilder.TryAdd(caseType, i))
            {
                throw new InvalidOperationException(
                    $"Union {unionType.FullName} declares case type {caseType.FullName} more than once.");
            }

            _cases[i] = CreateCaseReflected(caseType, ctors[i], root);
        }

        // Built once at formatter construction, looked up on every Serialize. Read-heavy
        // write-never is exactly the FrozenDictionary scenario; lookup is allocation-free
        // and typically faster than a regular Dictionary at this size.
        _caseIndexByType = indexBuilder.ToFrozenDictionary();

        _valueGetter = CompileValueGetter();
    }

    public void Serialize(ref BcsWriter writer, TUnion value)
    {
        var payload = _valueGetter(value)
            ?? throw new InvalidOperationException(
                $"Union {typeof(TUnion).FullName} value is uninitialized (Value is null).");

        var runtimeType = payload.GetType();

        // Exact-type match. Inherited types do not silently coerce to a declared case —
        // that would write the wrong variant index. Callers must use exactly one of the
        // declared case types.
        if (!_caseIndexByType.TryGetValue(runtimeType, out var idx))
        {
            throw new InvalidOperationException(
                $"Runtime type {runtimeType.FullName} does not match any declared case of union {typeof(TUnion).FullName}.");
        }

        writer.WriteULEB((uint)idx);
        _cases[idx].Write(ref writer, payload);
    }

    public TUnion Deserialize(ref BcsReader reader)
    {
        if (CountsTowardDepth)
        {
            reader.EnterContainer();
        }

        var idx = reader.ReadULEB32();
        if (idx >= (uint)_cases.Length)
        {
            throw new InvalidOperationException(
                $"Variant index {idx} is out of range for union {typeof(TUnion).FullName} (expected 0..{_cases.Length - 1}).");
        }

        var value = _cases[idx].Read(ref reader);

        if (CountsTowardDepth)
        {
            reader.LeaveContainer();
        }

        return value;
    }

    private static UnionCase CreateCaseReflected(Type caseType, ConstructorInfo ctor, IFormatterResolver? root)
    {
        try
        {
            return (UnionCase)CreateCaseMethod.MakeGenericMethod(caseType).Invoke(null, [ctor, root])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Surface the case-level diagnostic, not the reflection wrapper around it.
            ExceptionDispatchInfo.Throw(ex.InnerException);
            throw;
        }
    }

    private static UnionCase CreateCase<TCase>(ConstructorInfo ctor, IFormatterResolver? root)
    {
        var construct = CompileCaseCtor<TCase>(ctor);

        if (IsUnitLike(typeof(TCase)))
        {
            var unitValue = construct(CreateUnitInstance<TCase>());
            return new UnionCase(
                static (ref BcsWriter writer, object payload) => { },
                (ref BcsReader reader) => unitValue);
        }

        var formatter = BcsSerializer.GetFormatter<TCase>(root)
            ?? throw new InvalidOperationException(
                $"No BCS formatter resolved for union case {typeof(TCase).FullName} of {typeof(TUnion).FullName}. " +
                "Mark the case type with [BcsStruct], register a formatter via CustomFormatterResolver, " +
                "or annotate it with [BcsFormatter(typeof(...))].");

        return new UnionCase(
            (ref BcsWriter writer, object payload) => formatter.Serialize(ref writer, (TCase)payload),
            (ref BcsReader reader) =>
            {
                var payload = formatter.Deserialize(ref reader);

                // A null here would build a union whose Value is null, which only surfaces at the
                // next Serialize — far from the formatter that caused it.
                // The IsValueType guard keeps `is null` from boxing TCase: the JIT folds that box
                // away when optimizing, but an unoptimized Debug build pays it on every read.
                if (!typeof(TCase).IsValueType && payload is null)
                {
                    throw new InvalidOperationException(
                        $"Case formatter for {typeof(TCase).FullName} returned null.");
                }

                return construct(payload);
            });
    }

    private static Func<TCase, TUnion> CompileCaseCtor<TCase>(ConstructorInfo ctor)
    {
        var payload = Expression.Parameter(typeof(TCase), "payload");
        return Expression.Lambda<Func<TCase, TUnion>>(Expression.New(ctor, payload), payload).Compile();
    }

    private static TCase CreateUnitInstance<TCase>()
    {
        var caseType = typeof(TCase);

        // Reuse a declared singleton such as None.Instance so reference identity survives a round trip.
        if (caseType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is TCase sharedField)
        {
            return sharedField;
        }

        if (caseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is TCase sharedProperty)
        {
            return sharedProperty;
        }

        var ctor = caseType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException(
                $"Unit-like union case {caseType.FullName} must expose a public parameterless constructor or a public static Instance.");

        return (TCase)ctor.Invoke(null);
    }

    private static Func<TUnion, object?> CompileValueGetter()
    {
        var valueProp = typeof(TUnion).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Union {typeof(TUnion).FullName} lacks a public Value property.");
        var p = Expression.Parameter(typeof(TUnion), "u");
        Expression access = Expression.Property(p, valueProp);
        if (access.Type != typeof(object))
        {
            access = Expression.Convert(access, typeof(object));
        }
        return Expression.Lambda<Func<TUnion, object?>>(access, p).Compile();
    }

    private static bool IsUnitLike(Type t)
    {
        if (t.GetFields(BindingFlags.Public | BindingFlags.Instance).Length > 0)
        {
            return false;
        }

        foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Record types auto-generate an EqualityContract property. Treat it as invisible.
            if (prop.Name == "EqualityContract")
            {
                continue;
            }
            return false;
        }

        return true;
    }

    private readonly record struct UnionCase(CaseWriter Write, CaseReader Read);
}
