using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace BcsSharp.SourceGenerator;

[Generator]
public sealed class BcsFormatterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all types marked with [BcsStruct]
        var bcsStructTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "BcsSharp.Core.Attributes.BcsStructAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => GetBcsStructInfo(ctx))
            .Where(static m => m is not null)
            .Collect();

        // Find all types marked with [BcsEnum] (variant enums)
        var bcsEnumTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "BcsSharp.Core.Attributes.BcsEnumAttribute",
                predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                transform: static (ctx, _) => GetBcsEnumInfo(ctx))
            .Where(static m => m is not null)
            .Collect();

        // Generate formatters and resolver
        var allTypes = bcsStructTypes.Combine(bcsEnumTypes);
        context.RegisterSourceOutput(allTypes, static (spc, types) => Execute(spc, types.Left!, types.Right!));
    }

    private static BcsStructInfo? GetBcsStructInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration)
            return null;

        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // Get all fields/properties marked with [BcsField]
        var fields = new List<BcsFieldInfo>();

        // Process fields
        foreach (var field in typeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            var bcsFieldAttr = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "BcsSharp.Core.Attributes.BcsFieldAttribute");

            if (bcsFieldAttr != null)
            {
                var order = bcsFieldAttr.ConstructorArguments.FirstOrDefault().Value as int? ?? 0;
                fields.Add(new BcsFieldInfo(field.Name, field.Type.ToDisplayString(), field.Type, order, true));
            }
        }

        // Process properties
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var bcsFieldAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "BcsSharp.Core.Attributes.BcsFieldAttribute");

            if (bcsFieldAttr != null)
            {
                var order = bcsFieldAttr.ConstructorArguments.FirstOrDefault().Value as int? ?? 0;
                fields.Add(new BcsFieldInfo(property.Name, property.Type.ToDisplayString(), property.Type, order, false));
            }
        }

        if (fields.Count == 0)
            return null;

        // Sort by order
        fields.Sort((a, b) => a.Order.CompareTo(b.Order));

        return new BcsStructInfo(
            typeSymbol.ToDisplayString(),
            typeSymbol.Name,
            typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
            fields,
            typeSymbol.IsValueType,
            typeSymbol
        );
    }

    private static BcsEnumInfo? GetBcsEnumInfoFromSyntax(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        // Check if the type has the BcsEnum attribute
        var hasBcsEnumAttr = typeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "BcsSharp.Core.Attributes.BcsEnumAttribute");

        if (!hasBcsEnumAttr)
            return null;

        // Discover all variant classes for this enum
        var variants = DiscoverEnumVariants(typeSymbol);
        if (variants.Count == 0)
            return null;

        return new BcsEnumInfo(
            typeSymbol.ToDisplayString(),
            typeSymbol.Name,
            typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
            variants,
            typeSymbol
        );
    }

    private static BcsEnumInfo? GetBcsEnumInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration)
            return null;

        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // Discover all variant classes for this enum
        var variants = DiscoverEnumVariants(typeSymbol);
        if (variants.Count == 0)
            return null;

        return new BcsEnumInfo(
            typeSymbol.ToDisplayString(),
            typeSymbol.Name,
            typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
            variants,
            typeSymbol
        );
    }

    private static List<BcsVariantInfo> DiscoverEnumVariants(INamedTypeSymbol enumBaseType)
    {
        var variants = new List<BcsVariantInfo>();
        uint nextIndex = 0;

        // Strategy 1: Look for nested types first (common pattern)
        var nestedTypes = enumBaseType.GetTypeMembers();
        foreach (var nestedType in nestedTypes)
        {
            var variantAttr = nestedType.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "BcsSharp.Core.Attributes.BcsEnumVariantAttribute");

            if (variantAttr == null)
                continue;

            if (IsVariantOfEnum(nestedType, enumBaseType))
            {
                uint index = nextIndex;
                if (variantAttr.ConstructorArguments.Length > 0 && variantAttr.ConstructorArguments[0].Value is uint explicitIndex)
                {
                    index = explicitIndex;
                }

                var dataProperties = DiscoverVariantDataProperties(nestedType);

                variants.Add(new BcsVariantInfo(
                    index,
                    nestedType.Name,
                    nestedType.ToDisplayString(),
                    nestedType,
                    dataProperties
                ));

                nextIndex = Math.Max(nextIndex, index + 1);
            }
        }

        // Strategy 2: Look for types in the same assembly (for standalone variant classes)
        if (variants.Count == 0)
        {
            var assembly = enumBaseType.ContainingAssembly;
            var allTypes = GetTypesInAssembly(assembly);

            foreach (var type in allTypes)
            {
                // Skip if it's the enum base type itself
                if (SymbolEqualityComparer.Default.Equals(type, enumBaseType))
                    continue;

                var variantAttr = type.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "BcsSharp.Core.Attributes.BcsEnumVariantAttribute");

                if (variantAttr == null)
                    continue;

                // Check if type implements/inherits from the enum base type
                if (IsVariantOfEnum(type, enumBaseType))
                {
                    uint index = nextIndex;
                    if (variantAttr.ConstructorArguments.Length > 0 && variantAttr.ConstructorArguments[0].Value is uint explicitIndex)
                    {
                        index = explicitIndex;
                    }

                    var dataProperties = DiscoverVariantDataProperties(type);

                    variants.Add(new BcsVariantInfo(
                        index,
                        type.Name,
                        type.ToDisplayString(),
                        type,
                        dataProperties
                    ));

                    nextIndex = Math.Max(nextIndex, index + 1);
                }
            }
        }

        return variants.OrderBy(v => v.Index).ToList();
    }

    private static bool IsVariantOfEnum(INamedTypeSymbol candidateType, INamedTypeSymbol enumBaseType)
    {
        // Check if the candidate type implements the enum interface or inherits from enum base
        if (enumBaseType.TypeKind == TypeKind.Interface)
        {
            return candidateType.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, enumBaseType));
        }
        else
        {
            var baseType = candidateType.BaseType;
            while (baseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, enumBaseType))
                    return true;
                baseType = baseType.BaseType;
            }
        }
        return false;
    }

    private static List<BcsVariantDataInfo> DiscoverVariantDataProperties(INamedTypeSymbol variantType)
    {
        var dataProperties = new List<BcsVariantDataInfo>();

        foreach (var property in variantType.GetMembers().OfType<IPropertySymbol>())
        {
            var dataAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "BcsSharp.Core.Attributes.BcsEnumDataAttribute");

            if (dataAttr == null)
                continue;

            int order = dataProperties.Count;
            if (dataAttr.ConstructorArguments.Length > 0 && dataAttr.ConstructorArguments[0].Value is int explicitOrder)
            {
                order = explicitOrder;
            }

            dataProperties.Add(new BcsVariantDataInfo(
                property.Name,
                property.Type.ToDisplayString(),
                property.Type,
                order
            ));
        }

        return dataProperties.OrderBy(p => p.Order).ToList();
    }

    private static IEnumerable<INamedTypeSymbol> GetTypesInAssembly(IAssemblySymbol assembly)
    {
        return GetTypesInNamespace(assembly.GlobalNamespace);
    }

    private static IEnumerable<INamedTypeSymbol> GetTypesInNamespace(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetTypesInNamespace(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<BcsStructInfo> structTypes, ImmutableArray<BcsEnumInfo> enumTypes)
    {
        
        if (structTypes.IsDefaultOrEmpty && enumTypes.IsDefaultOrEmpty)
            return;

        var sb = new StringBuilder();
        var allStructTypes = new Dictionary<string, BcsStructInfo>();

        // Build dictionary of all known struct types using fully qualified name
        foreach (var type in structTypes)
        {
            var fullyQualifiedName = type.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            allStructTypes[fullyQualifiedName] = type;
        }

        // Dictionary types are now handled inline, no need for separate formatters

        // Generate individual struct formatters
        foreach (var type in structTypes)
        {
            sb.Clear();
            GenerateFormatter(sb, type, allStructTypes);
            // Use full type name to avoid duplicates when types have same name in different namespaces
            var hintName = type.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter.g.cs";
            context.AddSource(hintName, sb.ToString());
        }

        // Generate individual enum variant formatters
        foreach (var enumType in enumTypes)
        {
            sb.Clear();
            GenerateVariantEnumFormatter(sb, enumType, allStructTypes);
            var hintName = enumType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "VariantEnumFormatter.g.cs";
            context.AddSource(hintName, sb.ToString());
        }

        // Generate Dictionary formatters for direct Dictionary serialization
        var dictionaryTypes = DiscoverDictionaryTypes(structTypes, enumTypes);
        foreach (var (fullTypeName, keyType, valueType) in dictionaryTypes)
        {
            sb.Clear();
            GenerateStandaloneDictionaryFormatter(sb, fullTypeName, keyType, valueType);
            var hintName = fullTypeName.Replace("global::", "global__").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "") + "DictionaryFormatter.g.cs";
            context.AddSource(hintName, sb.ToString());
        }

        // Generate resolver
        sb.Clear();
        GenerateResolver(sb, structTypes, enumTypes);
        context.AddSource("BcsSourceGeneratorResolver.g.cs", sb.ToString());

        // Generate assembly attribute file
        sb.Clear();
        GenerateAssemblyAttribute(sb);
        context.AddSource("BcsGeneratedAssemblyAttribute.g.cs", sb.ToString());
    }

    private static void GenerateFormatter(StringBuilder sb, BcsStructInfo type, Dictionary<string, BcsStructInfo> allTypes)
    {
        var namespaceName = string.IsNullOrEmpty(type.Namespace) ? "BcsSharp.Generated" : $"{type.Namespace}.Generated";

        sb.AppendLine($@"
// <auto-generated />
#nullable enable

using BcsSharp.Core;

namespace {namespaceName};
");

        // Use sanitized type name to avoid conflicts
        var formatterClassName = type.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";

        sb.AppendLine($@"
public sealed class {formatterClassName} : IBcsFormatter<{type.FullTypeName}>
{{
    public static readonly {formatterClassName} Instance = new();
    public Type TargetType => typeof({type.FullTypeName});

    public void Serialize(ref BcsWriter writer, {type.FullTypeName} value)
    {{
");


        foreach (var field in type.Fields)
        {
            var writeCall = GetDirectWriteCall(field, allTypes);
            sb.AppendLine($"        // {field.Name}");
            sb.AppendLine($"        {writeCall}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // Deserialize method
        sb.AppendLine($"    public {type.FullTypeName} Deserialize(ref BcsReader reader)");
        sb.AppendLine("    {");

        if (type.IsValueType)
        {
            // For structs, use object initializer
            sb.AppendLine($"        return new {type.FullTypeName}");
            sb.AppendLine("        {");
            for (int i = 0; i < type.Fields.Count; i++)
            {
                var field = type.Fields[i];
                var comma = i < type.Fields.Count - 1 ? "," : "";
                var readCall = GetDirectReadCall(field, allTypes);
                sb.AppendLine($"            // {field.Name}");
                sb.AppendLine($"            {field.Name} = {readCall}{comma}");
            }
            sb.AppendLine("        };");
        }
        else
        {
            // For classes, create instance and set properties
            sb.AppendLine($"        var result = new {type.FullTypeName}();");
            foreach (var field in type.Fields)
            {
                var readCall = GetDirectReadCall(field, allTypes);
                sb.AppendLine($"        // {field.Name}");
                sb.AppendLine($"        result.{field.Name} = {readCall};");
            }
            sb.AppendLine("        return result;");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // GetSerializedSize method - simplified to return null for now
        sb.AppendLine($"    public int? GetSerializedSize({type.FullTypeName} value)");
        sb.AppendLine("    {");
        sb.AppendLine("        return null; // Size calculation not implemented yet");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add helper methods for primitive arrays if needed
        if (type.Fields.Any(f => GetFullyQualifiedTypeName(f.TypeSymbol).EndsWith("[]") && IsPrimitiveArrayType(GetFullyQualifiedTypeName(f.TypeSymbol))))
        {
            GeneratePrimitiveArrayHelpers(sb);
        }

        // Add helper methods for nullable arrays if needed
        var nullableArrayFields = type.Fields.Where(f => IsNullableArrayType(GetFullyQualifiedTypeName(f.TypeSymbol))).ToList();
        if (nullableArrayFields.Any())
        {
            GenerateNullableArrayHelpers(sb, nullableArrayFields);
        }

        // Add helper methods for enums if needed
        var enumFields = type.Fields.Where(f => f.TypeSymbol.TypeKind == TypeKind.Enum).ToList();
        if (enumFields.Any())
        {
            GenerateEnumHelpers(sb, enumFields);
        }

        // Add helper methods for Dictionary types if needed
        var dictionaryFields = type.Fields.Where(f => IsDictionaryType(f.TypeSymbol, out _, out _)).ToList();
        if (dictionaryFields.Any())
        {
            GenerateDictionaryHelpers(sb, dictionaryFields, allTypes);
        }

        sb.AppendLine("}");
    }

    private static string GetFullyQualifiedTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string GetDirectWriteCall(BcsFieldInfo field, Dictionary<string, BcsStructInfo> allTypes)
    {
        var fullyQualifiedTypeName = GetFullyQualifiedTypeName(field.TypeSymbol);

        // Handle Dictionary types first - inline serialization
        if (IsDictionaryType(field.TypeSymbol, out var keyType, out var valueType))
        {
            var methodName = GetDictionaryHelperMethodName(keyType, valueType, "Write");
            return $"{methodName}(ref writer, value.{field.Name});";
        }

        // Handle nullable primitive types first
        if (IsNullableType(fullyQualifiedTypeName, out var underlyingType))
        {
            return GetNullableWriteCall(field, underlyingType);
        }

        // Handle C-style enums - serialize by underlying integer value
        if (field.TypeSymbol.TypeKind == TypeKind.Enum)
        {
            return GetEnumWriteCall(field);
        }

        // Handle strings - always use WriteString regardless of nullable annotation
        if (fullyQualifiedTypeName == "string" || fullyQualifiedTypeName == "global::System.String" ||
            fullyQualifiedTypeName == "string?" || fullyQualifiedTypeName == "global::System.String?")
        {
            return $"writer.WriteString(value.{field.Name});";
        }

        return fullyQualifiedTypeName switch
        {
            // Primitive types - direct BcsWriter calls (handle both simple and fully qualified names)
            "byte" or "global::System.Byte" => $"writer.Write(value.{field.Name});",
            "sbyte" or "global::System.SByte" => $"writer.Write(value.{field.Name});",
            "ushort" or "global::System.UInt16" => $"writer.Write(value.{field.Name});",
            "short" or "global::System.Int16" => $"writer.Write(value.{field.Name});",
            "uint" or "global::System.UInt32" => $"writer.Write(value.{field.Name});",
            "int" or "global::System.Int32" => $"writer.Write(value.{field.Name});",
            "ulong" or "global::System.UInt64" => $"writer.Write(value.{field.Name});",
            "long" or "global::System.Int64" => $"writer.Write(value.{field.Name});",
            "UInt128" or "global::System.UInt128" => $"writer.Write(value.{field.Name});",
            "Int128" or "global::System.Int128" => $"writer.Write(value.{field.Name});",
            "global::Nethermind.Int256.UInt256" => $"writer.Write(value.{field.Name});",
            "bool" or "global::System.Boolean" => $"writer.WriteBool(value.{field.Name});",


            // Arrays of primitives - check if we can use vectorized operations
            "bool[]" or "global::System.Boolean[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "byte[]" or "global::System.Byte[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "sbyte[]" or "global::System.SByte[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "ushort[]" or "global::System.UInt16[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "short[]" or "global::System.Int16[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "uint[]" or "global::System.UInt32[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "int[]" or "global::System.Int32[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "ulong[]" or "global::System.UInt64[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "long[]" or "global::System.Int64[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "UInt128[]" or "global::System.UInt128[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "Int128[]" or "global::System.Int128[]" => GetFormatterPrimitiveArrayWriteCall(field),
            "global::Nethermind.Int256.UInt256[]" => GetFormatterPrimitiveArrayWriteCall(field),

            // Arrays of nullable primitives - custom handling
            "byte?[]" or "global::System.Byte?[]" => GetNullableArrayWriteCall(field, "byte"),
            "sbyte?[]" or "global::System.SByte?[]" => GetNullableArrayWriteCall(field, "sbyte"),
            "ushort?[]" or "global::System.UInt16?[]" => GetNullableArrayWriteCall(field, "ushort"),
            "short?[]" or "global::System.Int16?[]" => GetNullableArrayWriteCall(field, "short"),
            "uint?[]" or "global::System.UInt32?[]" => GetNullableArrayWriteCall(field, "uint"),
            "int?[]" or "global::System.Int32?[]" => GetNullableArrayWriteCall(field, "int"),
            "ulong?[]" or "global::System.UInt64?[]" => GetNullableArrayWriteCall(field, "ulong"),
            "long?[]" or "global::System.Int64?[]" => GetNullableArrayWriteCall(field, "long"),
            "UInt128?[]" or "global::System.UInt128?[]" => GetNullableArrayWriteCall(field, "UInt128"),
            "Int128?[]" or "global::System.Int128?[]" => GetNullableArrayWriteCall(field, "Int128"),
            "bool?[]" or "global::System.Boolean?[]" => GetNullableArrayWriteCall(field, "bool"),

            // Check if it's a OneOf<None, T> type where T is a known BcsStruct
            _ when IsOneOfNoneType(field.TypeSymbol, allTypes, out var innerTypeInfo) => GetOneOfFormatterWriteCall(field, innerTypeInfo!),

            // Check if it's a tuple type
            _ when IsTupleType(fullyQualifiedTypeName, out var tupleTypes) => GetTupleWriteCall(field, tupleTypes, allTypes),

            // Check if it's a known BcsStruct type - use dedicated formatter
            _ when allTypes.ContainsKey(GetFullyQualifiedTypeName(field.TypeSymbol)) => GetFormatterWriteCall(field, allTypes[GetFullyQualifiedTypeName(field.TypeSymbol)]),

            // Fall back to BcsSerializer for unknown complex types
            _ => $"BcsSerializer.Serialize(ref writer, value.{field.Name});"
        };
    }

    private static string GetFormatterPrimitiveArrayWriteCall(BcsFieldInfo field)
    {
        var fullyQualifiedTypeName = GetFullyQualifiedTypeName(field.TypeSymbol);
        // Extract the element type by removing the [] suffix and global:: prefix for primitive types
        var elementType = fullyQualifiedTypeName.Replace("[]", "").Replace("global::System.", "").Replace("global::", "");
        return $"writer.WriteULEB((uint)value.{field.Name}.Length); writer.WritePrimitiveArray<{elementType}>(value.{field.Name});";
    }

    private static string GetNullableArrayWriteCall(BcsFieldInfo field, string elementType)
    {
        var nullableWriteCall = GetNullableElementWriteCall(elementType);

        return $@"if (value.{field.Name} == null) {{ writer.WriteULEB(0u); }} else {{ writer.WriteULEB((uint)value.{field.Name}.Length); foreach (var item in value.{field.Name}) {{ {nullableWriteCall} }} }}";
    }

    private static string GetNullableElementWriteCall(string elementType)
    {
        return elementType switch
        {
            "byte" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "sbyte" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "ushort" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "short" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "uint" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "int" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "ulong" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "long" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "UInt128" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "Int128" => "if (item.HasValue) { writer.Write((byte)1); writer.Write(item.Value); } else { writer.Write((byte)0); }",
            "bool" => "if (item.HasValue) { writer.Write((byte)1); writer.WriteBool(item.Value); } else { writer.Write((byte)0); }",
            _ => "if (item.HasValue) { writer.Write((byte)1); BcsSerializer.Serialize(ref writer, item.Value); } else { writer.Write((byte)0); }"
        };
    }

    private static string GetNullableArrayReadCall(BcsFieldInfo field, string elementType)
    {
        var nullableReadCall = GetNullableElementReadCall(elementType);

        return $@"ReadNullableArray_{elementType}(ref reader)";
    }

    private static string GetNullableElementReadCall(string elementType)
    {
        return elementType switch
        {
            "byte" => "reader.Read8() == 0 ? null : reader.Read8()",
            "sbyte" => "reader.Read8() == 0 ? null : reader.ReadI8()",
            "ushort" => "reader.Read8() == 0 ? null : reader.Read16()",
            "short" => "reader.Read8() == 0 ? null : reader.ReadI16()",
            "uint" => "reader.Read8() == 0 ? null : reader.Read32()",
            "int" => "reader.Read8() == 0 ? null : reader.ReadI32()",
            "ulong" => "reader.Read8() == 0 ? null : reader.Read64()",
            "long" => "reader.Read8() == 0 ? null : reader.ReadI64()",
            "UInt128" => "reader.Read8() == 0 ? null : reader.Read128()",
            "Int128" => "reader.Read8() == 0 ? null : reader.ReadI128()",
            "bool" => "reader.Read8() == 0 ? null : reader.ReadBool()",
            _ => $"reader.Read8() == 0 ? null : BcsSerializer.Deserialize<{elementType}>(ref reader)"
        };
    }

    private static string GetFormatterWriteCall(BcsFieldInfo field, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.Serialize(ref writer, value.{field.Name});";
    }

    private static string GetFormatterReadCall(BcsFieldInfo field, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.Deserialize(ref reader)";
    }

    private static string GetFormatterSizeCall(BcsFieldInfo field, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.GetSerializedSize(value.{field.Name})";
    }

    private static bool IsOneOfNoneType(ITypeSymbol typeSymbol, Dictionary<string, BcsStructInfo> allTypes, out BcsStructInfo? innerTypeInfo)
    {
        innerTypeInfo = null;

        // Check if it's a generic OneOf type
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.TypeArguments.Length == 2)
        {
            var typeName = namedType.ToDisplayString();

            // Check if it contains OneOf and None patterns
            if (typeName.Contains("OneOf") && typeName.Contains("None"))
            {
                // Find the non-None type argument
                foreach (var typeArg in namedType.TypeArguments)
                {
                    var typeArgDisplay = typeArg.ToDisplayString();
                    if (!typeArgDisplay.Contains("None"))
                    {
                        var fullyQualifiedName = GetFullyQualifiedTypeName(typeArg);
                        if (allTypes.ContainsKey(fullyQualifiedName))
                        {
                            innerTypeInfo = allTypes[fullyQualifiedName];
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool IsNullableType(string typeName, out string underlyingType)
    {
        underlyingType = "";

        // Handle Nullable<T> pattern, but exclude string? since strings are reference types
        if (typeName.EndsWith("?") && typeName != "string?" && typeName != "global::System.String?")
        {
            underlyingType = typeName.Substring(0, typeName.Length - 1);
            return true;
        }

        // Handle System.Nullable<T> pattern
        if (typeName.StartsWith("System.Nullable<") && typeName.EndsWith(">"))
        {
            underlyingType = typeName.Substring("System.Nullable<".Length, typeName.Length - "System.Nullable<".Length - 1);
            return true;
        }

        // Handle global::System.Nullable<T> pattern
        if (typeName.StartsWith("global::System.Nullable<") && typeName.EndsWith(">"))
        {
            underlyingType = typeName.Substring("global::System.Nullable<".Length, typeName.Length - "global::System.Nullable<".Length - 1);
            return true;
        }

        return false;
    }

    private static string GetNullableWriteCall(BcsFieldInfo field, string underlyingType)
    {
        var underlyingWriteCall = underlyingType switch
        {
            "byte" or "global::System.Byte" => $"writer.Write(value.{field.Name}.Value);",
            "sbyte" or "global::System.SByte" => $"writer.Write(value.{field.Name}.Value);",
            "ushort" or "global::System.UInt16" => $"writer.Write(value.{field.Name}.Value);",
            "short" or "global::System.Int16" => $"writer.Write(value.{field.Name}.Value);",
            "uint" or "global::System.UInt32" => $"writer.Write(value.{field.Name}.Value);",
            "int" or "global::System.Int32" => $"writer.Write(value.{field.Name}.Value);",
            "ulong" or "global::System.UInt64" => $"writer.Write(value.{field.Name}.Value);",
            "long" or "global::System.Int64" => $"writer.Write(value.{field.Name}.Value);",
            "UInt128" or "System.UInt128" or "global::System.UInt128" => $"writer.Write(value.{field.Name}.Value);",
            "Int128" or "System.Int128" or "global::System.Int128" => $"writer.Write(value.{field.Name}.Value);",
            "bool" or "global::System.Boolean" => $"writer.WriteBool(value.{field.Name}.Value);",
            _ => $"BcsSerializer.Serialize(ref writer, value.{field.Name}.Value);"
        };

        return $"if (value.{field.Name}.HasValue) {{ writer.Write((byte)1); {underlyingWriteCall} }} else {{ writer.Write((byte)0); }}";
    }

    private static string GetNullableReadCall(BcsFieldInfo field, string underlyingType)
    {
        var underlyingReadCall = underlyingType switch
        {
            "byte" or "global::System.Byte" => "reader.Read8()",
            "sbyte" or "global::System.SByte" => "reader.ReadI8()",
            "ushort" or "global::System.UInt16" => "reader.Read16()",
            "short" or "global::System.Int16" => "reader.ReadI16()",
            "uint" or "global::System.UInt32" => "reader.Read32()",
            "int" or "global::System.Int32" => "reader.ReadI32()",
            "ulong" or "global::System.UInt64" => "reader.Read64()",
            "long" or "global::System.Int64" => "reader.ReadI64()",
            "UInt128" or "System.UInt128" or "global::System.UInt128" => "reader.Read128()",
            "Int128" or "System.Int128" or "global::System.Int128" => "reader.ReadI128()",
            "bool" or "global::System.Boolean" => "reader.ReadBool()",
            _ => $"BcsSerializer.Deserialize<{underlyingType}>(ref reader)"
        };

        return $"reader.Read8() == 0 ? null : {underlyingReadCall}";
    }

    private static string GetEnumWriteCall(BcsFieldInfo field)
    {
        if (field.TypeSymbol is not INamedTypeSymbol enumSymbol)
        {
            // Fallback for unknown enum types
            return $"BcsSerializer.Serialize(ref writer, value.{field.Name});";
        }

        var enumTypeName = GetFullyQualifiedTypeName(field.TypeSymbol);

        // BCS C-style enum serialization: Use ordinal position (index) within enum definition
        // This ensures consistent serialization regardless of actual enum values
        return $"WriteEnumOrdinal_{GetEnumHelperSuffix(enumTypeName)}(ref writer, value.{field.Name});";
    }

    private static string GetEnumReadCall(BcsFieldInfo field)
    {
        if (field.TypeSymbol is not INamedTypeSymbol enumSymbol)
        {
            // Fallback for unknown enum types
            return $"BcsSerializer.Deserialize<{GetFullyQualifiedTypeName(field.TypeSymbol)}>(ref reader)";
        }

        var enumTypeName = GetFullyQualifiedTypeName(field.TypeSymbol);

        // BCS C-style enum deserialization: Read ordinal position and convert to enum value
        return $"ReadEnumOrdinal_{GetEnumHelperSuffix(enumTypeName)}(ref reader)";
    }

    private static string GetEnumHelperSuffix(string enumTypeName)
    {
        // Create a safe suffix for method names by replacing problematic characters
        return enumTypeName.Replace(".", "_").Replace("::", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
    }

    private static bool IsTupleType(string typeName, out string[] tupleTypes)
    {
        tupleTypes = [];

        // Handle ValueTuple<T1, T2, ...> pattern
        if (typeName.StartsWith("(") && typeName.EndsWith(")"))
        {
            // Extract tuple elements from (T1, T2, ...)
            var content = typeName.Substring(1, typeName.Length - 2);
            tupleTypes = content.Split(',').Select(t => t.Trim()).ToArray();
            return tupleTypes.Length >= 2;
        }

        // Handle System.ValueTuple<T1, T2, ...> pattern
        if (typeName.StartsWith("System.ValueTuple<") && typeName.EndsWith(">"))
        {
            var content = typeName.Substring("System.ValueTuple<".Length, typeName.Length - "System.ValueTuple<".Length - 1);
            tupleTypes = content.Split(',').Select(t => t.Trim()).ToArray();
            return tupleTypes.Length >= 2;
        }

        return false;
    }

    private static string GetTupleWriteCall(BcsFieldInfo field, string[] tupleTypes, Dictionary<string, BcsStructInfo> allTypes)
    {
        var writeStatements = new List<string>();

        for (int i = 0; i < tupleTypes.Length; i++)
        {
            var itemAccess = $"value.{field.Name}.Item{i + 1}";
            var writeCall = GetTypeWriteCall(tupleTypes[i], itemAccess, allTypes);
            writeStatements.Add(writeCall);
        }

        return string.Join(" ", writeStatements);
    }

    private static string GetTupleReadCall(BcsFieldInfo field, string[] tupleTypes, Dictionary<string, BcsStructInfo> allTypes)
    {
        var readExpressions = new List<string>();

        for (int i = 0; i < tupleTypes.Length; i++)
        {
            var readCall = GetTypeReadCall(tupleTypes[i], allTypes);
            readExpressions.Add(readCall);
        }

        return $"({string.Join(", ", readExpressions)})";
    }

    private static string GetTypeWriteCall(string typeName, string valueExpression, Dictionary<string, BcsStructInfo> allTypes)
    {
        return typeName switch
        {
            // Primitive types (handle both simple and fully qualified names)
            "byte" or "global::System.Byte" => $"writer.Write({valueExpression});",
            "sbyte" or "global::System.SByte" => $"writer.Write({valueExpression});",
            "ushort" or "global::System.UInt16" => $"writer.Write({valueExpression});",
            "short" or "global::System.Int16" => $"writer.Write({valueExpression});",
            "uint" or "global::System.UInt32" => $"writer.Write({valueExpression});",
            "int" or "global::System.Int32" => $"writer.Write({valueExpression});",
            "ulong" or "global::System.UInt64" => $"writer.Write({valueExpression});",
            "long" or "global::System.Int64" => $"writer.Write({valueExpression});",
            "bool" or "global::System.Boolean" => $"writer.WriteBool({valueExpression});",
            "string" or "global::System.String" => $"writer.WriteString({valueExpression});",
            "string?" or "global::System.String?" => $"if ({valueExpression} == null) {{ writer.Write((byte)0); }} else {{ writer.Write((byte)1); writer.WriteString({valueExpression}); }}",

            // Check if it's a known BcsStruct type
            _ when allTypes.ContainsKey(typeName) => GetFormatterWriteCallForExpression(typeName, valueExpression, allTypes[typeName]),

            // Fall back to BcsSerializer
            _ => $"BcsSerializer.Serialize(ref writer, {valueExpression});"
        };
    }

    private static string GetTypeReadCall(string typeName, Dictionary<string, BcsStructInfo> allTypes)
    {
        return typeName switch
        {
            // Primitive types (handle both simple and fully qualified names)
            "byte" or "global::System.Byte" => "reader.Read8()",
            "sbyte" or "global::System.SByte" => "reader.ReadI8()",
            "ushort" or "global::System.UInt16" => "reader.Read16()",
            "short" or "global::System.Int16" => "reader.ReadI16()",
            "uint" or "global::System.UInt32" => "reader.Read32()",
            "int" or "global::System.Int32" => "reader.ReadI32()",
            "ulong" or "global::System.UInt64" => "reader.Read64()",
            "long" or "global::System.Int64" => "reader.ReadI64()",
            "bool" or "global::System.Boolean" => "reader.ReadBool()",
            "string" or "global::System.String" => "reader.ReadString()",
            "string?" or "global::System.String?" => "reader.Read8() == 0 ? null : reader.ReadString()",

            // Check if it's a known BcsStruct type
            _ when allTypes.ContainsKey(typeName) => GetFormatterReadCallForType(typeName, allTypes[typeName]),

            // Fall back to BcsSerializer
            _ => $"BcsSerializer.Deserialize<{typeName}>(ref reader)"
        };
    }

    private static string GetFormatterWriteCallForExpression(string typeName, string valueExpression, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.Serialize(ref writer, {valueExpression});";
    }

    private static string GetFormatterReadCallForType(string typeName, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.Deserialize(ref reader)";
    }

    private static string GetOneOfFormatterWriteCall(BcsFieldInfo field, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";

        return $"if (value.{field.Name}.IsT0) {{ writer.Write((byte)0); }} else if (value.{field.Name}.IsT1) {{ writer.Write((byte)1); {namespaceName}.{formatterClassName}.Instance.Serialize(ref writer, value.{field.Name}.AsT1); }}";
    }

    private static string GetOneOfFormatterReadCall(BcsFieldInfo field, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";

        return $"reader.Read8() == 0 ? new OneOf.Types.None() : {namespaceName}.{formatterClassName}.Instance.Deserialize(ref reader)";
    }

    private static string GetDirectReadCall(BcsFieldInfo field, Dictionary<string, BcsStructInfo> allTypes)
    {
        var fullyQualifiedTypeName = GetFullyQualifiedTypeName(field.TypeSymbol);

        // Handle Dictionary types first - inline deserialization
        if (IsDictionaryType(field.TypeSymbol, out var keyType, out var valueType))
        {
            var methodName = GetDictionaryHelperMethodName(keyType, valueType, "Read");
            return $"{methodName}(ref reader)";
        }

        // Handle nullable primitive types first
        if (IsNullableType(fullyQualifiedTypeName, out var underlyingType))
        {
            return GetNullableReadCall(field, underlyingType);
        }

        // Handle C-style enums - deserialize from underlying integer value
        if (field.TypeSymbol.TypeKind == TypeKind.Enum)
        {
            return GetEnumReadCall(field);
        }

        // Handle strings - always use ReadString regardless of nullable annotation
        if (fullyQualifiedTypeName == "string" || fullyQualifiedTypeName == "global::System.String" ||
            fullyQualifiedTypeName == "string?" || fullyQualifiedTypeName == "global::System.String?")
        {
            return "reader.ReadString()";
        }

        return fullyQualifiedTypeName switch
        {
            // Primitive types - direct BcsReader calls (handle both simple and fully qualified names)
            "byte" or "global::System.Byte" => "reader.Read8()",
            "sbyte" or "global::System.SByte" => "reader.ReadI8()",
            "ushort" or "global::System.UInt16" => "reader.Read16()",
            "short" or "global::System.Int16" => "reader.ReadI16()",
            "uint" or "global::System.UInt32" => "reader.Read32()",
            "int" or "global::System.Int32" => "reader.ReadI32()",
            "ulong" or "global::System.UInt64" => "reader.Read64()",
            "long" or "global::System.Int64" => "reader.ReadI64()",
            "UInt128" or "global::System.UInt128" => "reader.Read128()",
            "Int128" or "global::System.Int128" => "reader.ReadI128()",
            "global::Nethermind.Int256.UInt256" => "reader.Read256()",
            "bool" or "global::System.Boolean" => "reader.ReadBool()",

            // Arrays of primitives - vectorized read
            "bool[]" or "global::System.Boolean[]" => "ReadPrimitiveArray<bool>(ref reader)",
            "byte[]" or "global::System.Byte[]" => "reader.ReadBytes((int)reader.ReadULEB32())",
            "sbyte[]" or "global::System.SByte[]" => "ReadPrimitiveArray<sbyte>(ref reader)",
            "ushort[]" or "global::System.UInt16[]" => "ReadPrimitiveArray<ushort>(ref reader)",
            "short[]" or "global::System.Int16[]" => "ReadPrimitiveArray<short>(ref reader)",
            "uint[]" or "global::System.UInt32[]" => "ReadPrimitiveArray<uint>(ref reader)",
            "int[]" or "global::System.Int32[]" => "ReadPrimitiveArray<int>(ref reader)",
            "ulong[]" or "global::System.UInt64[]" => "ReadPrimitiveArray<ulong>(ref reader)",
            "long[]" or "global::System.Int64[]" => "ReadPrimitiveArray<long>(ref reader)",
            "UInt128[]" or "global::System.UInt128[]" => "ReadPrimitiveArray<System.UInt128>(ref reader)",
            "Int128[]" or "global::System.Int128[]" => "ReadPrimitiveArray<System.Int128>(ref reader)",
            "global::Nethermind.Int256.UInt256[]" => "ReadPrimitiveArray<Nethermind.Int256.UInt256>(ref reader)",

            // Arrays of nullable primitives - custom handling
            "byte?[]" or "global::System.Byte?[]" => GetNullableArrayReadCall(field, "byte"),
            "sbyte?[]" or "global::System.SByte?[]" => GetNullableArrayReadCall(field, "sbyte"),
            "ushort?[]" or "global::System.UInt16?[]" => GetNullableArrayReadCall(field, "ushort"),
            "short?[]" or "global::System.Int16?[]" => GetNullableArrayReadCall(field, "short"),
            "uint?[]" or "global::System.UInt32?[]" => GetNullableArrayReadCall(field, "uint"),
            "int?[]" or "global::System.Int32?[]" => GetNullableArrayReadCall(field, "int"),
            "ulong?[]" or "global::System.UInt64?[]" => GetNullableArrayReadCall(field, "ulong"),
            "long?[]" or "global::System.Int64?[]" => GetNullableArrayReadCall(field, "long"),
            "UInt128?[]" or "global::System.UInt128?[]" => GetNullableArrayReadCall(field, "UInt128"),
            "Int128?[]" or "global::System.Int128?[]" => GetNullableArrayReadCall(field, "Int128"),
            "bool?[]" or "global::System.Boolean?[]" => GetNullableArrayReadCall(field, "bool"),

            // Check if it's a OneOf<None, T> type where T is a known BcsStruct
            _ when IsOneOfNoneType(field.TypeSymbol, allTypes, out var innerTypeInfo) => GetOneOfFormatterReadCall(field, innerTypeInfo!),

            // Check if it's a tuple type
            _ when IsTupleType(fullyQualifiedTypeName, out var tupleTypes) => GetTupleReadCall(field, tupleTypes, allTypes),

            // Check if it's a known BcsStruct type - use dedicated formatter
            _ when allTypes.ContainsKey(GetFullyQualifiedTypeName(field.TypeSymbol)) => GetFormatterReadCall(field, allTypes[GetFullyQualifiedTypeName(field.TypeSymbol)]),

            // Fall back to BcsSerializer for unknown complex types
            _ => $"BcsSerializer.Deserialize<{fullyQualifiedTypeName}>(ref reader)"
        };
    }

    private static bool IsPrimitiveArrayType(string typeName)
    {
        return typeName switch
        {
            // Handle both simple and fully qualified names
            "bool[]" or "global::System.Boolean[]" or
            "byte[]" or "global::System.Byte[]" or
            "sbyte[]" or "global::System.SByte[]" or
            "ushort[]" or "global::System.UInt16[]" or
            "short[]" or "global::System.Int16[]" or
            "uint[]" or "global::System.UInt32[]" or
            "int[]" or "global::System.Int32[]" or
            "ulong[]" or "global::System.UInt64[]" or
            "long[]" or "global::System.Int64[]" or
            "UInt128[]" or "global::System.UInt128[]" or
            "Int128[]" or "global::System.Int128[]" or
            "global::Nethermind.Int256.UInt256[]" => true,
            _ => false
        };
    }

    private static bool IsNullableArrayType(string typeName)
    {
        return typeName switch
        {
            "byte?[]" or "global::System.Byte?[]" or
            "sbyte?[]" or "global::System.SByte?[]" or
            "ushort?[]" or "global::System.UInt16?[]" or
            "short?[]" or "global::System.Int16?[]" or
            "uint?[]" or "global::System.UInt32?[]" or
            "int?[]" or "global::System.Int32?[]" or
            "ulong?[]" or "global::System.UInt64?[]" or
            "long?[]" or "global::System.Int64?[]" or
            "UInt128?[]" or "global::System.UInt128?[]" or
            "Int128?[]" or "global::System.Int128?[]" or
            "bool?[]" or "global::System.Boolean?[]" => true,
            _ => false
        };
    }

    private static void GeneratePrimitiveArrayHelpers(StringBuilder sb)
    {
        sb.AppendLine("    // Helper methods for primitive arrays");
        sb.AppendLine("    private static T[] ReadPrimitiveArray<T>(ref BcsReader reader) where T : unmanaged");
        sb.AppendLine("    {");
        sb.AppendLine("        var length = (int)reader.ReadULEB32();");
        sb.AppendLine("        if (length == 0) return [];");
        sb.AppendLine("        ");
        sb.AppendLine("        var result = new T[length];");
        sb.AppendLine("        var byteSpan = System.Runtime.InteropServices.MemoryMarshal.AsBytes(result.AsSpan());");
        sb.AppendLine("        var readSpan = reader.ReadBytesAsSpan(byteSpan.Length);");
        sb.AppendLine("        readSpan.CopyTo(byteSpan);");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateNullableArrayHelpers(StringBuilder sb, List<BcsFieldInfo> nullableArrayFields)
    {
        sb.AppendLine("    // Helper methods for nullable arrays");

        // Get unique element types from the fields
        var elementTypes = nullableArrayFields
            .Select(f => GetFullyQualifiedTypeName(f.TypeSymbol).Replace("?[]", "").Replace("global::System.", "").Replace("global::", ""))
            .Distinct()
            .ToList();

        foreach (var elementType in elementTypes)
        {
            var readCall = GetNullableElementReadCall(elementType);

            sb.AppendLine($"    private static {elementType}?[] ReadNullableArray_{elementType}(ref BcsReader reader)");
            sb.AppendLine("    {");
            sb.AppendLine("        var length = (int)reader.ReadULEB32();");
            sb.AppendLine("        if (length == 0) return null;");
            sb.AppendLine($"        var array = new {elementType}?[length];");
            sb.AppendLine("        for (int i = 0; i < length; i++)");
            sb.AppendLine("        {");
            sb.AppendLine($"            array[i] = {readCall};");
            sb.AppendLine("        }");
            sb.AppendLine("        return array;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private static void GenerateEnumHelpers(StringBuilder sb, List<BcsFieldInfo> enumFields)
    {
        sb.AppendLine("    // Helper methods for C-style enum serialization (by ordinal position)");

        // Get unique enum types from the fields
        var enumTypes = enumFields
            .Select(f => new { TypeSymbol = f.TypeSymbol, TypeName = GetFullyQualifiedTypeName(f.TypeSymbol) })
            .GroupBy(x => x.TypeName)
            .Select(g => g.First())
            .ToList();

        foreach (var enumType in enumTypes)
        {
            var enumTypeName = enumType.TypeName;
            var suffix = GetEnumHelperSuffix(enumTypeName);

            if (enumType.TypeSymbol is not INamedTypeSymbol namedTypeSymbol)
                continue;

            // Get enum members in declaration order
            var enumMembers = namedTypeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .OrderBy(f => f.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
                .ToList();

            // Generate static cached enum values array
            sb.AppendLine($"    private static readonly {enumTypeName}[] _enumValues_{suffix} = new {enumTypeName}[]");
            sb.AppendLine("    {");
            for (int i = 0; i < enumMembers.Count; i++)
            {
                var comma = i < enumMembers.Count - 1 ? "," : "";
                sb.AppendLine($"        {enumTypeName}.{enumMembers[i].Name}{comma}");
            }
            sb.AppendLine("    };");
            sb.AppendLine();

            // Generate write method
            sb.AppendLine($"    private static void WriteEnumOrdinal_{suffix}(ref BcsWriter writer, {enumTypeName} value)");
            sb.AppendLine("    {");
            sb.AppendLine($"        // BCS C-style enum serialization: Use ordinal position (index) within enum definition");
            sb.AppendLine($"        var position = Array.IndexOf(_enumValues_{suffix}, value);");
            sb.AppendLine("        if (position == -1)");
            sb.AppendLine("        {");
            sb.AppendLine($"            throw new InvalidOperationException($\"Enum value {{value}} not found in {enumTypeName}\");");
            sb.AppendLine("        }");
            sb.AppendLine("        // Always serialize as byte representing the ordinal position");
            sb.AppendLine("        writer.Write((byte)position);");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate read method
            sb.AppendLine($"    private static {enumTypeName} ReadEnumOrdinal_{suffix}(ref BcsReader reader)");
            sb.AppendLine("    {");
            sb.AppendLine($"        // BCS C-style enum deserialization: Read ordinal position and convert to enum value");
            sb.AppendLine("        var position = reader.Read8();");
            sb.AppendLine($"        if (position >= _enumValues_{suffix}.Length)");
            sb.AppendLine("        {");
            sb.AppendLine($"            throw new InvalidOperationException($\"Invalid enum position {{position}} for {enumTypeName}. Enum has {{_enumValues_{suffix}.Length}} values (0-{{_enumValues_{suffix}.Length - 1}})\");");
            sb.AppendLine("        }");
            sb.AppendLine($"        return _enumValues_{suffix}[position];");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private static bool IsDictionaryType(ITypeSymbol typeSymbol, out string keyType, out string valueType)
    {
        keyType = "";
        valueType = "";

        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeDefinition = namedType.ConstructedFrom;
            var fullTypeName = typeDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Check for Dictionary<TKey, TValue>
            if (fullTypeName == "global::System.Collections.Generic.Dictionary<TKey, TValue>" && namedType.TypeArguments.Length == 2)
            {
                keyType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                valueType = namedType.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return true;
            }
        }

        return false;
    }

    private static string GetDictionaryHelperMethodName(string keyType, string valueType, string operation)
    {
        var keyTypeSafe = keyType.Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "");
        var valueTypeSafe = valueType.Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "");
        return $"{operation}Dictionary_{keyTypeSafe}__{valueTypeSafe}";
    }

    private static void GenerateDictionaryHelpers(StringBuilder sb, List<BcsFieldInfo> dictionaryFields, Dictionary<string, BcsStructInfo> allTypes)
    {
        var generatedMethods = new HashSet<string>();

        sb.AppendLine("    // Helper methods for Dictionary serialization/deserialization");

        foreach (var field in dictionaryFields)
        {
            if (IsDictionaryType(field.TypeSymbol, out var keyType, out var valueType))
            {
                var writeMethodName = GetDictionaryHelperMethodName(keyType, valueType, "Write");
                var readMethodName = GetDictionaryHelperMethodName(keyType, valueType, "Read");

                // Generate write method
                if (!generatedMethods.Contains(writeMethodName))
                {
                    GenerateDictionaryWriteMethod(sb, writeMethodName, keyType, valueType, allTypes);
                    generatedMethods.Add(writeMethodName);
                }

                // Generate read method
                if (!generatedMethods.Contains(readMethodName))
                {
                    GenerateDictionaryReadMethod(sb, readMethodName, keyType, valueType, allTypes);
                    generatedMethods.Add(readMethodName);
                }
            }
        }
    }

    private static void GenerateDictionaryWriteMethod(StringBuilder sb, string methodName, string keyType, string valueType, Dictionary<string, BcsStructInfo> allTypes)
    {
        var dictTypeName = $"global::System.Collections.Generic.Dictionary<{keyType}, {valueType}>";
        
        sb.AppendLine($"    private static void {methodName}(ref BcsWriter writer, {dictTypeName}? dict)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (dict == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteULEB(0u);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        writer.WriteULEB((uint)dict.Count);");
        sb.AppendLine();
        sb.AppendLine("        // Serialize key-value pairs and sort by lexicographical order of key bytes (BCS requirement)");
        sb.AppendLine("        var serializedPairs = new List<(byte[] keyBytes, byte[] valueBytes)>(dict.Count);");
        sb.AppendLine("        var tempWriter = new BcsWriter(new BcsWriterOptions { InitialBufferSize = 256 });");
        sb.AppendLine();
        sb.AppendLine("        foreach (var kvp in dict)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Serialize key");
        sb.AppendLine("            tempWriter.Reset();");
        sb.AppendLine($"            {GetInlineSerializeCall("tempWriter", "kvp.Key", keyType, allTypes)};");
        sb.AppendLine("            var keyBytes = tempWriter.ToBytes();");
        sb.AppendLine();
        sb.AppendLine("            // Serialize value");
        sb.AppendLine("            tempWriter.Reset();");
        sb.AppendLine($"            {GetInlineSerializeCall("tempWriter", "kvp.Value", valueType, allTypes)};");
        sb.AppendLine("            var valueBytes = tempWriter.ToBytes();");
        sb.AppendLine();
        sb.AppendLine("            serializedPairs.Add((keyBytes, valueBytes));");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Sort by lexicographical order of serialized key bytes");
        sb.AppendLine("        serializedPairs.Sort((a, b) => a.keyBytes.AsSpan().SequenceCompareTo(b.keyBytes.AsSpan()));");
        sb.AppendLine();
        sb.AppendLine("        // Write sorted key-value pairs");
        sb.AppendLine("        foreach (var pair in serializedPairs)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteBytes(pair.keyBytes);");
        sb.AppendLine("            writer.WriteBytes(pair.valueBytes);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateDictionaryReadMethod(StringBuilder sb, string methodName, string keyType, string valueType, Dictionary<string, BcsStructInfo> allTypes)
    {
        var dictTypeName = $"global::System.Collections.Generic.Dictionary<{keyType}, {valueType}>";
        
        sb.AppendLine($"    private static {dictTypeName} {methodName}(ref BcsReader reader)");
        sb.AppendLine("    {");
        sb.AppendLine("        var count = reader.ReadULEB32();");
        sb.AppendLine("        if (count == 0)");
        sb.AppendLine($"            return new {dictTypeName}();");
        sb.AppendLine();
        sb.AppendLine($"        var result = new {dictTypeName}((int)count);");
        sb.AppendLine("        byte[]? previousKeyBytes = null;");
        sb.AppendLine();
        sb.AppendLine("        for (uint i = 0; i < count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var key = {GetInlineDeserializeCall("reader", keyType, allTypes)};");
        sb.AppendLine($"            var value = {GetInlineDeserializeCall("reader", valueType, allTypes)};");
        sb.AppendLine();
        sb.AppendLine("            // Verify keys are in sorted order (BCS requirement)");
        sb.AppendLine("            if (i > 0 && previousKeyBytes != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                var keyWriter = new BcsWriter();");
        sb.AppendLine($"                {GetInlineSerializeCall("keyWriter", "key", keyType, allTypes)};");
        sb.AppendLine("                var currentKeyBytes = keyWriter.ToBytes();");
        sb.AppendLine();
        sb.AppendLine("                if (currentKeyBytes.AsSpan().SequenceCompareTo(previousKeyBytes.AsSpan()) <= 0)");
        sb.AppendLine("                {");
        sb.AppendLine("                    throw new InvalidOperationException(\"Map keys must be in strictly increasing lexicographical order by BCS bytes\");");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                previousKeyBytes = currentKeyBytes;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (i == 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                var keyWriter = new BcsWriter();");
        sb.AppendLine($"                {GetInlineSerializeCall("keyWriter", "key", keyType, allTypes)};");
        sb.AppendLine("                previousKeyBytes = keyWriter.ToBytes();");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (result.ContainsKey(key))");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new InvalidOperationException($\"Duplicate key found in map: {key}\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            result.Add(key, value);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string GetInlineSerializeCall(string writerName, string valueName, string typeName, Dictionary<string, BcsStructInfo> allTypes)
    {
        // Check if it's a known struct type
        if (allTypes.ContainsKey(typeName))
        {
            var structType = allTypes[typeName];
            var formatterClassName = structType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
            var namespaceName = string.IsNullOrEmpty(structType.Namespace) ? "BcsSharp.Generated" : $"{structType.Namespace}.Generated";
            return $"{namespaceName}.{formatterClassName}.Instance.Serialize(ref {writerName}, {valueName})";
        }

        // Handle primitive types
        return typeName switch
        {
            "global::System.String" or "string" => $"{writerName}.WriteString({valueName})",
            "global::System.Byte" or "byte" => $"{writerName}.Write({valueName})",
            "global::System.SByte" or "sbyte" => $"{writerName}.Write({valueName})",
            "global::System.UInt16" or "ushort" => $"{writerName}.Write({valueName})",
            "global::System.Int16" or "short" => $"{writerName}.Write({valueName})",
            "global::System.UInt32" or "uint" => $"{writerName}.Write({valueName})",
            "global::System.Int32" or "int" => $"{writerName}.Write({valueName})",
            "global::System.UInt64" or "ulong" => $"{writerName}.Write({valueName})",
            "global::System.Int64" or "long" => $"{writerName}.Write({valueName})",
            "global::System.Boolean" or "bool" => $"{writerName}.WriteBool({valueName})",
            _ => $"BcsSerializer.Serialize(ref {writerName}, {valueName})"
        };
    }

    private static string GetInlineDeserializeCall(string readerName, string typeName, Dictionary<string, BcsStructInfo> allTypes)
    {
        // Check if it's a known struct type
        if (allTypes.ContainsKey(typeName))
        {
            var structType = allTypes[typeName];
            var formatterClassName = structType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
            var namespaceName = string.IsNullOrEmpty(structType.Namespace) ? "BcsSharp.Generated" : $"{structType.Namespace}.Generated";
            return $"{namespaceName}.{formatterClassName}.Instance.Deserialize(ref {readerName})";
        }

        // Handle primitive types
        return typeName switch
        {
            "global::System.String" or "string" => $"{readerName}.ReadString()",
            "global::System.Byte" or "byte" => $"{readerName}.Read8()",
            "global::System.SByte" or "sbyte" => $"{readerName}.ReadI8()",
            "global::System.UInt16" or "ushort" => $"{readerName}.Read16()",
            "global::System.Int16" or "short" => $"{readerName}.ReadI16()",
            "global::System.UInt32" or "uint" => $"{readerName}.Read32()",
            "global::System.Int32" or "int" => $"{readerName}.ReadI32()",
            "global::System.UInt64" or "ulong" => $"{readerName}.Read64()",
            "global::System.Int64" or "long" => $"{readerName}.ReadI64()",
            "global::System.Boolean" or "bool" => $"{readerName}.ReadBool()",
            _ => $"BcsSerializer.Deserialize<{typeName}>(ref {readerName})"
        };
    }

    private static List<(string fullTypeName, string keyType, string valueType)> DiscoverDictionaryTypes(ImmutableArray<BcsStructInfo> structTypes, ImmutableArray<BcsEnumInfo> enumTypes)
    {
        var dictionaryTypes = new HashSet<(string fullTypeName, string keyType, string valueType)>();

        // Check all struct fields for Dictionary types
        foreach (var structType in structTypes)
        {
            foreach (var field in structType.Fields)
            {
                DiscoverDictionaryTypesFromSymbol(field.TypeSymbol, dictionaryTypes);
            }
        }

        // Check all enum variant data properties for Dictionary types
        foreach (var enumType in enumTypes)
        {
            foreach (var variant in enumType.Variants)
            {
                foreach (var dataProp in variant.DataProperties)
                {
                    DiscoverDictionaryTypesFromSymbol(dataProp.TypeSymbol, dictionaryTypes);
                }
            }
        }

        return dictionaryTypes.ToList();
    }

    private static void DiscoverDictionaryTypesFromSymbol(ITypeSymbol typeSymbol, HashSet<(string fullTypeName, string keyType, string valueType)> dictionaryTypes)
    {
        // Check if this is a Dictionary type
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeDefinition = namedType.ConstructedFrom;
            var fullTypeName = typeDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Check for Dictionary<TKey, TValue>
            if (fullTypeName == "global::System.Collections.Generic.Dictionary<TKey, TValue>" && namedType.TypeArguments.Length == 2)
            {
                var keyType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var valueType = namedType.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var dictFullTypeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
                dictionaryTypes.Add((dictFullTypeName, keyType, valueType));
            }
        }

        // Recursively check generic type arguments
        if (typeSymbol is INamedTypeSymbol namedTypeGeneric && namedTypeGeneric.IsGenericType)
        {
            foreach (var typeArg in namedTypeGeneric.TypeArguments)
            {
                DiscoverDictionaryTypesFromSymbol(typeArg, dictionaryTypes);
            }
        }
    }



    private static void GenerateResolver(StringBuilder sb, ImmutableArray<BcsStructInfo> structTypes, ImmutableArray<BcsEnumInfo> enumTypes)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using BcsSharp.Core;");
        sb.AppendLine();
        sb.AppendLine("namespace BcsSharp.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Source-generated resolver that provides optimized formatters for [BcsStruct] types.");
        sb.AppendLine("/// An instance of this resolver that only returns formatters specifically generated for types in this assembly.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed partial class BcsSourceGeneratorResolver : IFormatterResolver");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>An instance of this resolver that only returns formatters specifically generated for types in this assembly.</summary>");
        sb.AppendLine("    public static readonly IFormatterResolver Instance = new BcsSourceGeneratorResolver();");
        sb.AppendLine();
        sb.AppendLine("    private BcsSourceGeneratorResolver()");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public IBcsFormatter<T>? GetFormatter<T>()");
        sb.AppendLine("    {");
        sb.AppendLine("        return FormatterCache<T>.Formatter;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static class FormatterCache<T>");
        sb.AppendLine("    {");
        sb.AppendLine("        internal static readonly IBcsFormatter<T>? Formatter;");
        sb.AppendLine();
        sb.AppendLine("        static FormatterCache()");
        sb.AppendLine("        {");
        sb.AppendLine("            var f = BcsSourceGeneratorResolverGetFormatterHelper.GetFormatter(typeof(T));");
        sb.AppendLine("            if (f != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                Formatter = (IBcsFormatter<T>)f;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate the GetFormatterHelper class
        GenerateGetFormatterHelper(sb, structTypes, enumTypes);

        sb.AppendLine("}");
    }

    private static void GenerateGetFormatterHelper(StringBuilder sb, ImmutableArray<BcsStructInfo> structTypes, ImmutableArray<BcsEnumInfo> enumTypes)
    {
        sb.AppendLine("    private static class BcsSourceGeneratorResolverGetFormatterHelper");
        sb.AppendLine("    {");
        
        // Generate the dictionary with all known types
        var allTypes = new List<(string fullTypeName, string formatterInstantiation, string namespaceName)>();
        
        foreach (var type in structTypes)
        {
            var namespaceName = string.IsNullOrEmpty(type.Namespace) ? "BcsSharp.Generated" : $"{type.Namespace}.Generated";
            var formatterClassName = type.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
            allTypes.Add((type.FullTypeName, $"new {namespaceName}.{formatterClassName}()", namespaceName));
        }

        foreach (var enumType in enumTypes)
        {
            var namespaceName = string.IsNullOrEmpty(enumType.Namespace) ? "BcsSharp.Generated" : $"{enumType.Namespace}.Generated";
            var formatterClassName = enumType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "VariantEnumFormatter";
            allTypes.Add((enumType.FullTypeName, $"new {namespaceName}.{formatterClassName}()", namespaceName));
        }

        // Add Dictionary formatters for direct Dictionary serialization
        var dictionaryTypes = DiscoverDictionaryTypes(structTypes, enumTypes);
        foreach (var (fullTypeName, keyType, valueType) in dictionaryTypes)
        {
            var formatterClassName = fullTypeName.Replace("global::", "global__").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "") + "DictionaryFormatter";
            allTypes.Add((fullTypeName, $"new BcsSharp.Generated.{formatterClassName}()", "BcsSharp.Generated"));
        }

        if (allTypes.Count > 0)
        {
            sb.AppendLine($"        private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, int> closedTypeLookup = new global::System.Collections.Generic.Dictionary<global::System.Type, int>({allTypes.Count})");
            sb.AppendLine("        {");
            
            for (int i = 0; i < allTypes.Count; i++)
            {
                var (fullTypeName, _, _) = allTypes[i];
                var comma = i < allTypes.Count - 1 ? "," : "";
                sb.AppendLine($"            {{ typeof({fullTypeName}), {i} }}{comma}");
            }
            
            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        internal static object? GetFormatter(global::System.Type t)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (closedTypeLookup.TryGetValue(t, out int closedKey))");
            sb.AppendLine("            {");
            sb.AppendLine("                switch (closedKey)");
            sb.AppendLine("                {");
            
            for (int i = 0; i < allTypes.Count; i++)
            {
                var (_, formatterInstantiation, _) = allTypes[i];
                sb.AppendLine($"                    case {i}: return {formatterInstantiation};");
            }
            
            sb.AppendLine("                    default: return null; // unreachable");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
        }
        else
        {
            // Handle case where no types are found
            sb.AppendLine("        internal static object? GetFormatter(global::System.Type t)");
            sb.AppendLine("        {");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
        }
        
        sb.AppendLine("    }");
    }

    private static void GenerateStandaloneDictionaryFormatter(StringBuilder sb, string fullTypeName, string keyType, string valueType)
    {
        var formatterClassName = fullTypeName.Replace("global::", "global__").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "") + "DictionaryFormatter";
        
        sb.AppendLine($@"
// <auto-generated />
#nullable enable

using BcsSharp.Core;
using System.Runtime.CompilerServices;

namespace BcsSharp.Generated;

public sealed class {formatterClassName} : IBcsFormatter<{fullTypeName}>
{{
    public static readonly {formatterClassName} Instance = new();
    public Type TargetType => typeof({fullTypeName});

    private static readonly BcsWriterOptions _internalWriterOptions = new()
    {{
        InitialBufferSize = 256
    }};

    public void Serialize(ref BcsWriter writer, {fullTypeName} value)
    {{
        if (value == null)
        {{
            writer.WriteULEB(0u);
            return;
        }}

        writer.WriteULEB((uint)value.Count);

        // Serialize key-value pairs and sort by lexicographical order of key bytes (BCS requirement)
        var serializedPairs = new List<(byte[] keyBytes, byte[] valueBytes)>(value.Count);
        var tempWriter = new BcsWriter(_internalWriterOptions);

        foreach (var kvp in value)
        {{
            // Serialize key
            tempWriter.Reset();
            {GetInlineSerializeCall("tempWriter", "kvp.Key", keyType, new Dictionary<string, BcsStructInfo>())};
            var keyBytes = tempWriter.ToBytes();

            // Serialize value  
            tempWriter.Reset();
            {GetInlineSerializeCall("tempWriter", "kvp.Value", valueType, new Dictionary<string, BcsStructInfo>())};
            var valueBytes = tempWriter.ToBytes();

            serializedPairs.Add((keyBytes, valueBytes));
        }}

        // Sort by lexicographical order of serialized key bytes
        serializedPairs.Sort((a, b) => CompareByteArrays(a.keyBytes, b.keyBytes));

        // Write sorted key-value pairs  
        foreach (var pair in serializedPairs)
        {{
            writer.WriteBytes(pair.keyBytes);
            writer.WriteBytes(pair.valueBytes);
        }}
    }}

    public {fullTypeName} Deserialize(ref BcsReader reader)
    {{
        var count = reader.ReadULEB32();
        if (count == 0)
            return new {fullTypeName}();

        var result = new {fullTypeName}((int)count);
        byte[]? previousKeyBytes = null;

        for (uint i = 0; i < count; i++)
        {{
            var key = {GetInlineDeserializeCall("reader", keyType, new Dictionary<string, BcsStructInfo>())};
            var value = {GetInlineDeserializeCall("reader", valueType, new Dictionary<string, BcsStructInfo>())};

            // Verify keys are in sorted order (BCS requirement)
            if (i > 0 && previousKeyBytes != null)
            {{
                var keyWriter = new BcsWriter();
                {GetInlineSerializeCall("keyWriter", "key", keyType, new Dictionary<string, BcsStructInfo>())};
                var currentKeyBytes = keyWriter.ToBytes();

                if (CompareByteArrays(currentKeyBytes, previousKeyBytes) <= 0)
                {{
                    throw new InvalidOperationException(""Map keys must be in strictly increasing lexicographical order by BCS bytes"");
                }}

                previousKeyBytes = currentKeyBytes;
            }}
            else if (i == 0)
            {{
                var keyWriter = new BcsWriter();
                {GetInlineSerializeCall("keyWriter", "key", keyType, new Dictionary<string, BcsStructInfo>())};
                previousKeyBytes = keyWriter.ToBytes();
            }}

            if (result.ContainsKey(key))
            {{
                throw new InvalidOperationException($""Duplicate key found in map: {{key}}"");
            }}

            result.Add(key, value);
        }}

        return result;
    }}

    public int? GetSerializedSize({fullTypeName} value)
    {{
        return null; // Size calculation not implemented yet
    }}

    /// <summary>
    /// Compares two byte arrays lexicographically
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CompareByteArrays(byte[] a, byte[] b)
    {{
        return a.AsSpan().SequenceCompareTo(b.AsSpan());
    }}
}}");
    }

    private static void GenerateAssemblyAttribute(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using BcsSharp.Core.Attributes;");
        sb.AppendLine("using BcsSharp.Generated;");
        sb.AppendLine();
        sb.AppendLine("[assembly: GeneratedAssemblyBcsResolverAttribute(typeof(BcsSourceGeneratorResolver))]");
    }

    private static void GenerateVariantEnumFormatter(StringBuilder sb, BcsEnumInfo enumType, Dictionary<string, BcsStructInfo> allStructTypes)
    {
        var namespaceName = string.IsNullOrEmpty(enumType.Namespace) ? "BcsSharp.Generated" : $"{enumType.Namespace}.Generated";
        var formatterClassName = enumType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "VariantEnumFormatter";

        sb.AppendLine($@"
// <auto-generated />
#nullable enable

using BcsSharp.Core;

namespace {namespaceName};

public sealed class {formatterClassName} : IBcsFormatter<{enumType.FullTypeName}>
{{
    public static readonly {formatterClassName} Instance = new();
    public Type TargetType => typeof({enumType.FullTypeName});

    public void Serialize(ref BcsWriter writer, {enumType.FullTypeName} value)
    {{
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var valueType = value.GetType();
        switch (valueType.Name)
        {{");

        // Generate serialize cases for each variant
        foreach (var variant in enumType.Variants)
        {
            sb.AppendLine($@"            case ""{variant.Name}"":
                writer.WriteULEB({variant.Index}u);");

            if (variant.DataProperties.Count > 0)
            {
                sb.AppendLine($"                var {variant.Name.ToLower()} = ({variant.FullTypeName})value;");
                foreach (var dataProp in variant.DataProperties)
                {
                    var writeCall = GetVariantDataWriteCall(dataProp, $"{variant.Name.ToLower()}", allStructTypes);
                    sb.AppendLine($"                {writeCall}");
                }
            }
            sb.AppendLine("                break;");
        }

        sb.AppendLine($@"            default:
                throw new InvalidOperationException($""Unknown variant type: {{valueType.Name}}"");
        }}
    }}

    public {enumType.FullTypeName} Deserialize(ref BcsReader reader)
    {{
        var variantIndex = reader.ReadULEB32();
        switch (variantIndex)
        {{");

        // Generate deserialize cases for each variant
        foreach (var variant in enumType.Variants)
        {
            sb.AppendLine($@"            case {variant.Index}u:");
            
            if (variant.DataProperties.Count == 0)
            {
                // Unit variant - just create instance
                sb.AppendLine($"                return new {variant.FullTypeName}();");
            }
            else
            {
                // Variant with data - create instance and populate properties
                sb.AppendLine($"                var instance{variant.Index} = new {variant.FullTypeName}();");
                foreach (var dataProp in variant.DataProperties)
                {
                    var readCall = GetVariantDataReadCall(dataProp, allStructTypes);
                    sb.AppendLine($"                instance{variant.Index}.{dataProp.Name} = {readCall};");
                }
                sb.AppendLine($"                return instance{variant.Index};");
            }
        }

        sb.AppendLine($@"            default:
                throw new InvalidOperationException($""Unknown variant index: {{variantIndex}}"");
        }}
    }}

    public int? GetSerializedSize({enumType.FullTypeName} value)
    {{
        return null; // Size calculation not implemented yet
    }}
}}");
    }

    private static string GetVariantDataWriteCall(BcsVariantDataInfo dataProp, string instanceName, Dictionary<string, BcsStructInfo> allStructTypes)
    {
        var fullyQualifiedTypeName = GetFullyQualifiedTypeName(dataProp.TypeSymbol);

        // Handle strings - always use WriteString regardless of nullable annotation  
        if (fullyQualifiedTypeName == "string" || fullyQualifiedTypeName == "global::System.String" ||
            fullyQualifiedTypeName == "string?" || fullyQualifiedTypeName == "global::System.String?")
        {
            return $"writer.WriteString({instanceName}.{dataProp.Name});";
        }

        return fullyQualifiedTypeName switch
        {
            // Primitive types - direct BcsWriter calls
            "byte" or "global::System.Byte" => $"writer.Write({instanceName}.{dataProp.Name});",
            "sbyte" or "global::System.SByte" => $"writer.Write({instanceName}.{dataProp.Name});",
            "ushort" or "global::System.UInt16" => $"writer.Write({instanceName}.{dataProp.Name});",
            "short" or "global::System.Int16" => $"writer.Write({instanceName}.{dataProp.Name});",
            "uint" or "global::System.UInt32" => $"writer.Write({instanceName}.{dataProp.Name});",
            "int" or "global::System.Int32" => $"writer.Write({instanceName}.{dataProp.Name});",
            "ulong" or "global::System.UInt64" => $"writer.Write({instanceName}.{dataProp.Name});",
            "long" or "global::System.Int64" => $"writer.Write({instanceName}.{dataProp.Name});",
            "bool" or "global::System.Boolean" => $"writer.WriteBool({instanceName}.{dataProp.Name});",

            // Check if it's a known BcsStruct type - use dedicated formatter
            _ when allStructTypes.ContainsKey(fullyQualifiedTypeName) => GetVariantFormatterWriteCall(dataProp, instanceName, allStructTypes[fullyQualifiedTypeName]),

            // Fall back to BcsSerializer for unknown complex types
            _ => $"BcsSerializer.Serialize(ref writer, {instanceName}.{dataProp.Name});"
        };
    }

    private static string GetVariantDataReadCall(BcsVariantDataInfo dataProp, Dictionary<string, BcsStructInfo> allStructTypes)
    {
        var fullyQualifiedTypeName = GetFullyQualifiedTypeName(dataProp.TypeSymbol);

        // Handle strings - always use ReadString regardless of nullable annotation
        if (fullyQualifiedTypeName == "string" || fullyQualifiedTypeName == "global::System.String" ||
            fullyQualifiedTypeName == "string?" || fullyQualifiedTypeName == "global::System.String?")
        {
            return "reader.ReadString()";
        }

        return fullyQualifiedTypeName switch
        {
            // Primitive types - direct BcsReader calls
            "byte" or "global::System.Byte" => "reader.Read8()",
            "sbyte" or "global::System.SByte" => "reader.ReadI8()",
            "ushort" or "global::System.UInt16" => "reader.Read16()",
            "short" or "global::System.Int16" => "reader.ReadI16()",
            "uint" or "global::System.UInt32" => "reader.Read32()",
            "int" or "global::System.Int32" => "reader.ReadI32()",
            "ulong" or "global::System.UInt64" => "reader.Read64()",
            "long" or "global::System.Int64" => "reader.ReadI64()",
            "bool" or "global::System.Boolean" => "reader.ReadBool()",

            // Check if it's a known BcsStruct type - use dedicated formatter
            _ when allStructTypes.ContainsKey(fullyQualifiedTypeName) => GetVariantFormatterReadCall(dataProp, allStructTypes[fullyQualifiedTypeName]),

            // Fall back to BcsSerializer for unknown complex types
            _ => $"BcsSerializer.Deserialize<{fullyQualifiedTypeName}>(ref reader)"
        };
    }

    private static string GetVariantFormatterWriteCall(BcsVariantDataInfo dataProp, string instanceName, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.Serialize(ref writer, {instanceName}.{dataProp.Name});";
    }

    private static string GetVariantFormatterReadCall(BcsVariantDataInfo dataProp, BcsStructInfo targetType)
    {
        var formatterClassName = targetType.FullTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_') + "Formatter";
        var namespaceName = string.IsNullOrEmpty(targetType.Namespace) ? "BcsSharp.Generated" : $"{targetType.Namespace}.Generated";
        return $"{namespaceName}.{formatterClassName}.Instance.Deserialize(ref reader)";
    }

}

// Data structures for BCS enum information
public class BcsEnumInfo
{
    public string FullTypeName { get; }
    public string Name { get; }
    public string Namespace { get; }
    public List<BcsVariantInfo> Variants { get; }
    public INamedTypeSymbol TypeSymbol { get; }

    public BcsEnumInfo(string fullTypeName, string name, string namespaceName, List<BcsVariantInfo> variants, INamedTypeSymbol typeSymbol)
    {
        FullTypeName = fullTypeName;
        Name = name;
        Namespace = namespaceName;
        Variants = variants;
        TypeSymbol = typeSymbol;
    }
}

public class BcsVariantInfo
{
    public uint Index { get; }
    public string Name { get; }
    public string FullTypeName { get; }
    public INamedTypeSymbol TypeSymbol { get; }
    public List<BcsVariantDataInfo> DataProperties { get; }

    public BcsVariantInfo(uint index, string name, string fullTypeName, INamedTypeSymbol typeSymbol, List<BcsVariantDataInfo> dataProperties)
    {
        Index = index;
        Name = name;
        FullTypeName = fullTypeName;
        TypeSymbol = typeSymbol;
        DataProperties = dataProperties;
    }
}

public class BcsVariantDataInfo
{
    public string Name { get; }
    public string TypeName { get; }
    public ITypeSymbol TypeSymbol { get; }
    public int Order { get; }

    public BcsVariantDataInfo(string name, string typeName, ITypeSymbol typeSymbol, int order)
    {
        Name = name;
        TypeName = typeName;
        TypeSymbol = typeSymbol;
        Order = order;
    }
}
