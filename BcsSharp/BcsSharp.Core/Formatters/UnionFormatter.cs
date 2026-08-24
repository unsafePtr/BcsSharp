using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// BCS formatter for C# 15 <c>union</c> types. Variant index = declaration order of the
/// compiler-synthesised single-argument case constructors. Each case payload is serialized
/// via its own formatter, resolved through the standard chain.
///
/// Case-payload resolution rules:
/// <list type="bullet">
///   <item>Unit-like cases (no instance fields/properties — e.g. an empty record class like
///   <c>None</c>) emit a zero-byte payload, matching Rust's unit-variant wire format.</item>
///   <item>Any other case type must be resolvable by the active formatter resolver (i.e.
///   primitive, <c>[BcsStruct]</c>, or registered manually).</item>
/// </list>
/// </summary>
/// <remarks>
/// The Value getter is compiled as a delegate against the union struct's <c>Value</c> property
/// directly, avoiding the 24 B/op boxing cost of casting the struct to <see cref="IUnion"/>.
/// </remarks>
public sealed class UnionFormatter<TUnion> : IBcsFormatter<TUnion>
{
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
            var isUnit = IsUnitLike(caseType);
            _cases[i] = new UnionCase
            {
                Type = caseType,
                IsUnit = isUnit,
                Constructor = CompileCtorDelegate(ctors[i]),
                Formatter = isUnit
                    ? null
                    : BcsSerializerExtensions.GetFormatter(caseType, root)
                      ?? throw new InvalidOperationException(
                          $"No BCS formatter resolved for union case {caseType.FullName} of {unionType.FullName}. " +
                          "Mark the case type with [BcsStruct], register a formatter via CustomFormatterResolver, " +
                          "or annotate it with [BcsFormatter(typeof(...))]."),
                UnitConstructor = isUnit ? CompileParameterlessCtor(caseType) : null,
            };

            if (!indexBuilder.TryAdd(caseType, i))
            {
                throw new InvalidOperationException(
                    $"Union {unionType.FullName} declares case type {caseType.FullName} more than once.");
            }
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
        ref readonly var caseInfo = ref _cases[idx];
        if (!caseInfo.IsUnit)
        {
            caseInfo.Formatter!.SerializeObject(ref writer, payload);
        }
    }

    public TUnion Deserialize(ref BcsReader reader)
    {
        var idx = reader.ReadULEB32();
        if (idx >= (uint)_cases.Length)
        {
            throw new InvalidOperationException(
                $"Variant index {idx} is out of range for union {typeof(TUnion).FullName} (expected 0..{_cases.Length - 1}).");
        }

        ref readonly var c = ref _cases[idx];
        var payload = c.IsUnit
            ? c.UnitConstructor!()
            : c.Formatter!.DeserializeObject(ref reader)
              ?? throw new InvalidOperationException(
                  $"Case formatter for {c.Type.FullName} returned null.");

        return c.Constructor(payload);
    }

    private static Func<object, TUnion> CompileCtorDelegate(ConstructorInfo ctor)
    {
        var paramType = ctor.GetParameters()[0].ParameterType;
        var p = Expression.Parameter(typeof(object), "payload");
        var cast = Expression.Convert(p, paramType);
        var newExpr = Expression.New(ctor, cast);
        return Expression.Lambda<Func<object, TUnion>>(newExpr, p).Compile();
    }

    private static Func<object> CompileParameterlessCtor(Type t)
    {
        var ctor = t.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException(
                $"Unit-like union case {t.FullName} must expose a public parameterless constructor.");
        var newExpr = Expression.New(ctor);
        var asObj = Expression.Convert(newExpr, typeof(object));
        return Expression.Lambda<Func<object>>(asObj).Compile();
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

    private struct UnionCase
    {
        public Type Type;
        public bool IsUnit;
        public Func<object, TUnion> Constructor;
        public Func<object>? UnitConstructor;
        public IBcsObjectFormatter? Formatter;
    }
}
