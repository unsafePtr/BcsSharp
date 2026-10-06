using System.Linq.Expressions;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// High-performance formatter for BCS-serializable objects (structs/classes) marked with [BcsStruct].
/// Dispatch to per-field typed <see cref="IBcsFormatter{T}"/>s goes through a virtual call on <see cref="BcsObjectFieldSerializer{TInstance}"/>, avoiding the <c>object?</c> boxing path of <see cref="IBcsObjectFormatter"/>.
/// Field accessors are compiled expression trees.
///
/// BCS Format: Fields are serialized consecutively in order without any headers or separators.
/// Example: struct Person { name: String, age: u32 } -> [name_data...] + [age_data...]
/// </summary>
/// <typeparam name="T">The object type to serialize.</typeparam>
public sealed class BcsObjectFormatter<T> : IBcsFormatter<T>
{
    private readonly BcsObjectFieldSerializer<T>[] _fields;
    private readonly Func<T> _constructor;
    private readonly bool _isValueType;

    private static readonly Type TypeCached = typeof(T);

    public BcsObjectFormatter() : this(null) { }

    public BcsObjectFormatter(IFormatterResolver? root)
    {
        var bcsStructAttr = TypeCached.GetCustomAttribute<BcsStructAttribute>();
        if (bcsStructAttr == null)
        {
            ThrowHelper.ThrowInvalidOperationException($"Type {TypeCached.Name} must be marked with [BcsStruct] attribute");
        }

        _isValueType = TypeCached.IsValueType;
        _fields = DiscoverAndCompileFields(TypeCached, root);
        _constructor = CreateConstructorDelegate();

        if (_fields.Length == 0)
        {
            ThrowHelper.ThrowInvalidOperationException($"Type {TypeCached.Name} has no serializable fields marked with [BcsField]");
        }
    }

    public void Serialize(ref BcsWriter writer, T value)
    {
        // Skip the null check for value types — it can't be null, and the
        // ArgumentNullException.ThrowIfNull(object?) overload boxes the struct just to
        // check `!= null`, costing ~24 B/op for no benefit.
        if (!_isValueType && value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        foreach (var field in _fields)
        {
            field.Serialize(ref writer, value);
        }
    }

    public T Deserialize(ref BcsReader reader)
    {
        reader.EnterContainer();

        var instance = _constructor();
        foreach (var field in _fields)
        {
            field.Deserialize(ref reader, ref instance);
        }

        reader.LeaveContainer();

        return instance;
    }

    public void Deserialize(ref BcsReader reader, ref T value)
    {
        reader.EnterContainer();

        if (!_isValueType && value is null)
        {
            value = _constructor();
        }

        foreach (var field in _fields)
        {
            field.Deserialize(ref reader, ref value);
        }

        reader.LeaveContainer();
    }

    private static Func<T> CreateConstructorDelegate()
    {
        if (typeof(T).IsValueType)
        {
            var newExpression = Expression.Default(typeof(T));
            return Expression.Lambda<Func<T>>(newExpression).Compile();
        }
        else
        {
            var constructor = typeof(T).GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"Type {typeof(T).Name} must have a parameterless constructor for BCS deserialization");
            var newExpression = Expression.New(constructor);
            return Expression.Lambda<Func<T>>(newExpression).Compile();
        }
    }

    private static BcsObjectFieldSerializer<T>[] DiscoverAndCompileFields(Type objectType, IFormatterResolver? root)
    {
        var serializers = new List<BcsObjectFieldSerializer<T>>();

        List<MemberInfo> members =
        [
            .. objectType.GetFields(BindingFlags.Public | BindingFlags.Instance),
            .. objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance),
        ];

        foreach (var member in members)
        {
            var fieldAttr = member.GetCustomAttribute<BcsFieldAttribute>();
            if (fieldAttr == null)
            {
                continue;
            }

            Type memberType;
            if (member is FieldInfo field)
            {
                memberType = field.FieldType;
            }
            else if (member is PropertyInfo property)
            {
                memberType = property.PropertyType;
                if (!property.CanRead)
                {
                    throw new InvalidOperationException($"Property {member.Name} in {objectType.Name} must be readable");
                }

                if (!property.CanWrite)
                {
                    throw new InvalidOperationException($"Property {member.Name} in {objectType.Name} must be writable");
                }
            }
            else
            {
                continue;
            }

            var serializer = CreateTypedFieldSerializer(member, memberType, root);
            serializer.Order = fieldAttr.Order;
            serializers.Add(serializer);
        }

        serializers.Sort((a, b) => a.Order.CompareTo(b.Order));
        return [.. serializers];
    }

    private static BcsObjectFieldSerializer<T> CreateTypedFieldSerializer(MemberInfo member, Type memberType, IFormatterResolver? root)
    {
        var memberName = member.Name;

        // Compile typed getter: Func<T, TField>
        var getterParam = Expression.Parameter(typeof(T), "instance");
        var getterAccess = Expression.PropertyOrField(getterParam, memberName);
        var getterDelegateType = typeof(Func<,>).MakeGenericType(typeof(T), memberType);
        var getter = Expression.Lambda(getterDelegateType, getterAccess, getterParam).Compile();

        // Compile typed ref-setter: RefSetter<T, TField>
        var setterInstanceParam = Expression.Parameter(typeof(T).MakeByRefType(), "instance");
        var setterValueParam = Expression.Parameter(memberType, "value");
        var setterAccess = Expression.PropertyOrField(setterInstanceParam, memberName);
        var setterAssign = Expression.Assign(setterAccess, setterValueParam);
        var setterDelegateType = typeof(RefSetter<,>).MakeGenericType(typeof(T), memberType);
        var setter = Expression.Lambda(setterDelegateType, setterAssign, setterInstanceParam, setterValueParam).Compile();

        // Resolve the typed IBcsFormatter<TField> directly (skip the boxed adapter).
        var typedFormatter = GetTypedFormatter(memberType, root)
            ?? throw new InvalidOperationException($"No BCS formatter found for field {memberName} of type {memberType.Name}");

        var serializerType = typeof(TypedBcsObjectFieldSerializer<,>).MakeGenericType(typeof(T), memberType);
        return (BcsObjectFieldSerializer<T>)Activator.CreateInstance(serializerType, getter, setter, typedFormatter)!;
    }

    private static object? GetTypedFormatter(Type type, IFormatterResolver? root)
    {
        return typeof(BcsSerializer).GetMethod(nameof(BcsSerializer.GetFormatter), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(type)
            .InvokeUnwrapped(null, [root]);
    }
}

/// <summary>
/// Non-generic-in-TField field serializer.
/// <see cref="BcsObjectFormatter{T}"/> holds an array of these so it can dispatch per-field via a single virtual call — no <c>object?</c> boxing of field values across the formatter boundary.
/// </summary>
/// <typeparam name="TInstance">The object type whose field is being serialized.</typeparam>
internal abstract class BcsObjectFieldSerializer<TInstance>
{
    public int Order { get; set; }
    public abstract void Serialize(ref BcsWriter writer, TInstance instance);
    public abstract void Deserialize(ref BcsReader reader, ref TInstance instance);
}

/// <summary>
/// Compiled ref-T setter: writes <typeparamref name="TField"/> directly into the field storage of <typeparamref name="TInstance"/> without boxing.
/// For struct instances the caller passes <c>ref instance</c>, so the assignment lands in the original storage.
/// </summary>
internal delegate void RefSetter<TInstance, TField>(ref TInstance instance, TField value);

internal sealed class TypedBcsObjectFieldSerializer<TInstance, TField> : BcsObjectFieldSerializer<TInstance>
{
    private readonly Func<TInstance, TField> _getter;
    private readonly RefSetter<TInstance, TField> _setter;
    private readonly IBcsFormatter<TField> _formatter;

    public TypedBcsObjectFieldSerializer(
        Func<TInstance, TField> getter,
        RefSetter<TInstance, TField> setter,
        IBcsFormatter<TField> formatter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    public override void Serialize(ref BcsWriter writer, TInstance instance)
    {
        var fieldValue = _getter(instance);
        _formatter.Serialize(ref writer, fieldValue);
    }

    public override void Deserialize(ref BcsReader reader, ref TInstance instance)
    {
        // Pass the current field value through so overriding formatters (BcsObject, List,
        // Dictionary) can reuse it; default-DIM formatters still allocate fresh.
        var existing = _getter(instance);
        _formatter.Deserialize(ref reader, ref existing);
        _setter(ref instance, existing);
    }
}
