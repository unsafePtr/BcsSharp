using Microsoft.CodeAnalysis;

namespace BcsSharp.SourceGenerator;

internal sealed class BcsStructInfo
{
    public string FullTypeName { get; }
    public string TypeName { get; }
    public string Namespace { get; }
    public List<BcsFieldInfo> Fields { get; }
    public bool IsValueType { get; }
    public INamedTypeSymbol TypeSymbol { get; }

    public BcsStructInfo(string fullTypeName, string typeName, string nameSpace, List<BcsFieldInfo> fields, bool isValueType, INamedTypeSymbol typeSymbol)
    {
        FullTypeName = fullTypeName;
        TypeName = typeName;
        Namespace = nameSpace;
        Fields = fields;
        IsValueType = isValueType;
        TypeSymbol = typeSymbol;
    }
}
