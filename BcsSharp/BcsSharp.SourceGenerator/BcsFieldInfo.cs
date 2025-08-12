using Microsoft.CodeAnalysis;

namespace BcsSharp.SourceGenerator;

internal sealed class BcsFieldInfo
{
    public string Name { get; }
    public string TypeName { get; }
    public ITypeSymbol TypeSymbol { get; }
    public int Order { get; }
    public bool IsField { get; }

    public BcsFieldInfo(string name, string typeName, ITypeSymbol typeSymbol, int order, bool isField)
    {
        Name = name;
        TypeName = typeName;
        TypeSymbol = typeSymbol;
        Order = order;
        IsField = isField;
    }
}