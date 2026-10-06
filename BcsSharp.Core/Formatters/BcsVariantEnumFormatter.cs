using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core.Formatters;

/// <summary>
/// High-performance formatter for Rust-style variant enums (tagged unions) following the official BCS specification.
/// Uses compiled expression trees for fast object creation and property access.
/// Uses ULEB128-encoded variant indices and supports associated data of any BCS type.
///
/// BCS Format: [ULEB128 variant_index] + [associated_data...]
/// Example: E::Variant2("hello") -> [2, 5, 'h', 'e', 'l', 'l', 'o']
/// </summary>
/// <typeparam name="T">The base enum type or interface.</typeparam>
public sealed class BcsVariantEnumFormatter<T> : IBcsFormatter<T>
{
    private readonly Dictionary<uint, BcsVariantInfo> _variantsByIndex;
    private readonly Dictionary<Type, BcsVariantInfo> _variantsByType;

    // Static cache for constructor delegates to avoid recompilation - local per formatter type
    private static readonly ConcurrentDictionary<Type, Func<object>> _constructorCache = new();

    private static readonly Type TypeCached = typeof(T);

    private readonly IFormatterResolver? _root;

    public BcsVariantEnumFormatter() : this(null) { }

    public BcsVariantEnumFormatter(IFormatterResolver? root)
    {
        _root = root;
        var bcsEnumAttr = TypeCached.GetCustomAttribute<BcsEnumAttribute>() ?? throw new InvalidOperationException($"Type {TypeCached.Name} must be marked with [BcsEnum] attribute");
        var variants = DiscoverVariants(TypeCached);

        _variantsByIndex = variants.ToDictionary(v => v.Index);
        _variantsByType = variants.ToDictionary(v => v.VariantType);
    }

    public void Serialize(ref BcsWriter writer, T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var valueType = value.GetType();

        if (!_variantsByType.TryGetValue(valueType, out var variant))
        {
            throw new InvalidOperationException($"Unknown variant type: {valueType.Name}");
        }

        // Write ULEB128 variant index (as per BCS specification)
        writer.WriteULEB(variant.Index);

        // Write associated data if present
        if (variant.DataProperties.Any())
        {
            SerializeVariantData(ref writer, value, variant);
        }
    }

    public T Deserialize(ref BcsReader reader)
    {
        reader.EnterContainer();

        // Read ULEB128 variant index
        var variantIndex = reader.ReadULEB32();

        if (!_variantsByIndex.TryGetValue(variantIndex, out var variant))
        {
            throw new InvalidOperationException($"Unknown variant index: {variantIndex}");
        }

        // Create instance of variant type using compiled constructor delegate
        var instance = variant.Constructor() ?? throw new InvalidOperationException($"Failed to create instance of {variant.VariantType.Name}");

        // Deserialize associated data if present
        if (variant.DataProperties.Any())
        {
            DeserializeVariantData(ref reader, instance, variant);
        }

        reader.LeaveContainer();

        return (T)instance;
    }

    /// <summary>
    /// Finds the variants nested in the marker, or failing that, declared anywhere in the marker's assembly.
    /// Nothing outside that assembly is searched: which assemblies are loaded depends on what ran first, so a wider search could cache a formatter missing variants, and any loaded assembly could add one.
    /// </summary>
    private List<BcsVariantInfo> DiscoverVariants(Type enumBaseType)
    {
        var variants = CollectVariants(enumBaseType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic), enumBaseType);

        if (variants.Count == 0)
        {
            variants = CollectVariants(enumBaseType.Assembly.GetTypes(), enumBaseType);
        }

        if (variants.Count == 0)
        {
            throw new InvalidOperationException(DescribeMissingVariants(enumBaseType));
        }

        // Ties are ordered by name so a duplicate index is reported the same way on every run.
        variants = [.. variants.OrderBy(v => v.Index).ThenBy(v => v.VariantType.FullName, StringComparer.Ordinal)];

        for (var i = 1; i < variants.Count; i++)
        {
            if (variants[i].Index == variants[i - 1].Index)
            {
                throw new InvalidOperationException($"{enumBaseType.Name} declares [BcsEnumVariant({variants[i].Index})] on both {variants[i - 1].Name} and {variants[i].Name}.");
            }
        }

        return variants;
    }

    private List<BcsVariantInfo> CollectVariants(Type[] candidates, Type enumBaseType)
    {
        var variants = new List<BcsVariantInfo>();

        foreach (var type in candidates)
        {
            var variantAttr = type.GetCustomAttribute<BcsEnumVariantAttribute>();
            if (variantAttr is null || !IsVariantOfEnum(type, enumBaseType))
            {
                continue;
            }

            variants.Add(new BcsVariantInfo
            {
                Index = variantAttr.Index,
                Name = type.Name,
                VariantType = type,
                Constructor = GetOrCreateConstructorDelegate(type),
                DataProperties = DiscoverDataProperties(type)
            });
        }

        return variants;
    }

    /// <summary>
    /// Loaded assemblies are searched only here, to name misplaced variants; the result never builds a formatter, so load order can only make the message less specific.
    /// </summary>
    private static string DescribeMissingVariants(Type enumBaseType)
    {
        var home = enumBaseType.Assembly;
        var homeName = home.GetName().Name;
        var problem = $"{enumBaseType.Name} has no [BcsEnumVariant] types in its own assembly {homeName}, the only place variants are discovered. ";

        var elsewhere = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly != home && !assembly.IsDynamic && References(assembly, homeName))
            .Select(assembly => (Assembly: assembly.GetName().Name, Variants: VariantNames(assembly, enumBaseType)))
            .Where(found => found.Variants.Length > 0)
            .OrderBy(found => found.Assembly, StringComparer.Ordinal)
            .Select(found => $"{found.Assembly} ({string.Join(", ", found.Variants)})")
            .ToArray();

        if (elsewhere.Length == 0)
        {
            return problem + $"Each variant must be declared there, implement {enumBaseType.Name} and carry [BcsEnumVariant(index)].";
        }

        return problem + $"Found in other assemblies: {string.Join("; ", elsewhere)}. Declare the variants next to the marker, or model the enum as a union.";
    }

    // A type implementing the marker can only live in an assembly that references the marker's, so the rest are skipped unread.
    private static bool References(Assembly assembly, string? name) =>
        assembly.GetReferencedAssemblies().Any(reference => reference.Name == name);

    private static string[] VariantNames(Assembly assembly, Type enumBaseType)
    {
        Type?[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // An assembly with a missing dependency still yields the types that did load.
            types = ex.Types;
        }

        return [.. types
            .OfType<Type>()
            .Where(type => IsVariantOfEnum(type, enumBaseType) && type.GetCustomAttribute<BcsEnumVariantAttribute>() is not null)
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)];
    }

    private static bool IsVariantOfEnum(Type candidateType, Type enumBaseType)
    {
        // Check if the type implements the enum interface/inherits from enum base
        // Also ensure it's not a generic type definition to avoid issues with generic variants
        return enumBaseType.IsAssignableFrom(candidateType) &&
               candidateType != enumBaseType &&
               !candidateType.IsGenericTypeDefinition;
    }

    private List<BcsDataProperty> DiscoverDataProperties(Type variantType)
    {
        var properties = new List<BcsDataProperty>();

        foreach (var prop in variantType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var dataAttr = prop.GetCustomAttribute<BcsEnumDataAttribute>();
            if (dataAttr == null)
            {
                continue;
            }

            var formatter = BcsSerializerExtensions.GetFormatter(prop.PropertyType, _root) ?? throw new InvalidOperationException($"No BCS formatter found for property {prop.Name} of type {prop.PropertyType.Name}");
            properties.Add(new BcsDataProperty
            {
                Property = prop,
                Order = dataAttr.Order ?? properties.Count,
                Formatter = formatter,
                GetValue = CompilePropertyGetter(prop),
                SetValue = CompilePropertySetter(prop)
            });
        }

        return [.. properties.OrderBy(p => p.Order)];
    }

    private static void SerializeVariantData(ref BcsWriter writer, object instance, BcsVariantInfo variant)
    {
        foreach (var dataProp in variant.DataProperties)
        {
            // Use compiled expression-based getter for optimal performance
            var value = dataProp.GetValue(instance);
            dataProp.Formatter.SerializeObject(ref writer, value);
        }
    }

    private static void DeserializeVariantData(ref BcsReader reader, object instance, BcsVariantInfo variant)
    {
        foreach (var dataProp in variant.DataProperties)
        {
            var value = dataProp.Formatter.DeserializeObject(ref reader);
            // Use compiled expression-based setter for optimal performance
            dataProp.SetValue(instance, value);
        }
    }

    /// <summary>
    /// Creates or retrieves a cached constructor delegate for fast object instantiation
    /// </summary>
    private static Func<object> GetOrCreateConstructorDelegate(Type type)
    {
        return _constructorCache.GetOrAdd(type, CreateConstructorDelegate);
    }

    /// <summary>
    /// Creates a high-performance constructor delegate using compiled expressions
    /// </summary>
    private static Func<object> CreateConstructorDelegate(Type type)
    {
        if (type.IsValueType)
        {
            // For value types, use default(T) and box it
            var defaultExpression = Expression.Default(type);
            var boxedExpression = Expression.Convert(defaultExpression, typeof(object));
            return Expression.Lambda<Func<object>>(boxedExpression).Compile();
        }
        else
        {
            // For reference types, use the parameterless constructor
            var constructor = type.GetConstructor(Type.EmptyTypes) ?? throw new InvalidOperationException($"Type {type.Name} must have a parameterless constructor for BCS deserialization");
            var newExpression = Expression.New(constructor);
            var convertExpression = Expression.Convert(newExpression, typeof(object));
            return Expression.Lambda<Func<object>>(convertExpression).Compile();
        }
    }

    /// <summary>
    /// Compiles a high-performance property getter using expression trees
    /// </summary>
    private static Func<object, object?> CompilePropertyGetter(PropertyInfo property)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var convertedObj = Expression.Convert(objParam, property.DeclaringType!);
        var propertyAccess = Expression.Property(convertedObj, property);
        var boxed = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, objParam).Compile();
    }

    /// <summary>
    /// Compiles a high-performance property setter using expression trees
    /// </summary>
    private static Action<object, object?> CompilePropertySetter(PropertyInfo property)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valueParam = Expression.Parameter(typeof(object), "value");
        var convertedObj = Expression.Convert(objParam, property.DeclaringType!);
        var convertedValue = Expression.Convert(valueParam, property.PropertyType);
        var propertyAccess = Expression.Property(convertedObj, property);
        var assignment = Expression.Assign(propertyAccess, convertedValue);
        return Expression.Lambda<Action<object, object?>>(assignment, objParam, valueParam).Compile();
    }

    private sealed class BcsVariantInfo
    {
        public uint Index { get; set; }
        public string Name { get; set; } = "";
        public Type VariantType { get; set; } = null!;
        public Func<object> Constructor { get; set; } = null!;
        public List<BcsDataProperty> DataProperties { get; set; } = new();
    }

    private sealed class BcsDataProperty
    {
        public PropertyInfo Property { get; set; } = null!;
        public int Order { get; set; }
        public IBcsObjectFormatter Formatter { get; set; } = null!;

        // Compiled expression-based accessors for maximum performance
        public Func<object, object?> GetValue { get; set; } = null!;
        public Action<object, object?> SetValue { get; set; } = null!;
    }
}

/// <summary>
/// Non-generic interface for object-based BCS formatting.
/// Used for enum variant data serialization where the type is known at runtime.
/// </summary>
internal interface IBcsObjectFormatter
{
    void SerializeObject(ref BcsWriter writer, object? value);
    object? DeserializeObject(ref BcsReader reader);
}

/// <summary>
/// Adapter to convert generic IBcsFormatter to object-based formatter
/// </summary>
internal sealed class BcsObjectFormatterAdapter<T> : IBcsObjectFormatter
{
    private readonly IBcsFormatter<T> _formatter;

    public BcsObjectFormatterAdapter(IBcsFormatter<T> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    public void SerializeObject(ref BcsWriter writer, object? value)
    {
        if (value is T typedValue)
        {
            _formatter.Serialize(ref writer, typedValue);
        }
        else if (value == null)
        {
            _formatter.Serialize(ref writer, default!);
        }
        else
        {
            throw new InvalidOperationException($"Value is not of expected type {typeof(T).Name}");
        }
    }

    public object? DeserializeObject(ref BcsReader reader)
    {
        return _formatter.Deserialize(ref reader);
    }
}

/// <summary>
/// Helper extension methods for BcsSerializer to support object-based formatting
/// </summary>
internal static class BcsSerializerExtensions
{
    public static IBcsObjectFormatter? GetFormatter(Type type, IFormatterResolver? root = null)
    {
        var formatter = typeof(BcsSerializer).GetMethod(nameof(BcsSerializer.GetFormatter), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(type)
            .InvokeUnwrapped(null, [root]); // null falls back to the default chain

        if (formatter == null)
        {
            return null;
        }

        var adapterType = typeof(BcsObjectFormatterAdapter<>).MakeGenericType(type);
        return (IBcsObjectFormatter?)Activator.CreateInstance(adapterType, formatter);
    }
}
