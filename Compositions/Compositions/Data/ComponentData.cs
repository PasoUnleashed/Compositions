using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Compositions.Data;

public class ComponentData : IData
{
    public INamedTypeSymbol Symbol;
    public List<MemberData> Members = new List<MemberData>();
    public List<INamedTypeSymbol> PartOfCompositions = new List<INamedTypeSymbol>();
    private string _interfaceName;

    public ComponentData(INamedTypeSymbol symbol)
    {
        Symbol = symbol;
    }

    public string GetInterfaceName()
    {
        if (string.IsNullOrEmpty(_interfaceName))
        {
            _interfaceName = $"I{GetName()}ComponentHaver";
        }

        return _interfaceName;
    }
    public string GetName()
    {
        return Symbol.Name.Replace("Component", "");
    }
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Symbol.ToString());
        sb.AppendLine(" Members:");
        foreach (var member in Members)
        {
            sb.AppendLine($"     {member.Name}:{member.Type}");
        }

        sb.AppendLine(" Part of:");
        foreach (var composition in PartOfCompositions)
        {
            sb.AppendLine($"    {composition.Name}");
        }

        return sb.ToString();
    }
}

public interface IData
{
}

public class MemberData
{
    public ITypeSymbol Type;
    public string Name;

    public MemberData(ITypeSymbol type, string name)
    {
        Type = type;
        Name = name;
    }
}