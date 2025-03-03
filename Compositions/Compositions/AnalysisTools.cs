using System.Collections.Generic;
using System.Linq;
using Compositions.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Compositions;

public static class AnalysisTools
{
    public static bool IsOfType(this INamedTypeSymbol symbol, string name, string hintName)
    {
        return symbol != null && symbol.Name == name && symbol.ContainingAssembly.Name.Contains(hintName);
    }

    public static IEnumerable<INamedTypeSymbol> GetComponentCompositionInclusions(this INamedTypeSymbol componentSymbol)
    {
        foreach (var data in componentSymbol.GetAllAttributes())
        {
            if (data.AttributeClass.IsInclusionAttribute())
            {
                if (data.ConstructorArguments.Length > 0)
                {
                    yield return data.ConstructorArguments.First().Value as INamedTypeSymbol;
                }
            }
        }
    }

    public static IEnumerable<AttributeData> GetAllAttributes(this INamedTypeSymbol symbol)
    {
        if (symbol == null)
        {
            yield break;
        }
        foreach (var data in symbol.GetAttributes())
        {
            yield return data;
        }

        foreach (var data in symbol.BaseType.GetAllAttributes())
        {
            yield return data;
        }
    }
}

public static class CompositionsAnalysisTools
{
    public static bool IsComposition(this INamedTypeSymbol symbol, List<IData> allData)
    {
        DebugTools.Log(symbol.Name);
        foreach (var allInterface in symbol.AllInterfaces)
        {
            DebugTools.Log(symbol.Name+" "+allInterface.Name);
            if (allInterface.IsOfType("IComposition", "Compositions"))
            {
                return true;
            }
        }
        foreach (var allInterface in symbol.Interfaces)
        {
            DebugTools.Log(symbol.Name+" "+allInterface.Name);
            if (allInterface.IsOfType("IComposition", "Compositions"))
            {
                return true;
            }
        }

        foreach (var data in allData.OfType<ComponentData>())
        {
            if (symbol.DeclaringSyntaxReferences.First() == null)
            {
                continue;
            }
            var syntaxNode = symbol.DeclaringSyntaxReferences.First().GetSyntax();
            if (syntaxNode is ClassDeclarationSyntax s)
            {

                return s.InheritsInterface(data.GetInterfaceName());
            }
        }
        return false;
    }

    public static bool IsComponent(this INamedTypeSymbol symbol)
    {
        foreach (var allInterface in symbol.AllInterfaces)
        {
            if (allInterface.IsOfType("IComponent", "Compositions"))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInclusionAttribute(this INamedTypeSymbol attributeSymbol)
    {
        return attributeSymbol.IsOfType("IncludedInCompositionAttribute", "Compositions");
    }

    public static bool InheritsInterface(this ClassDeclarationSyntax syntax,string interfaceName)
    {
        if (syntax == null || syntax.BaseList == null || syntax.BaseList.Types == null)
        {
            return false;
        }
        foreach (var baseTypeSyntax in syntax.BaseList.Types)
        {
            if (baseTypeSyntax.ToString().Split('<')[0].Contains(interfaceName))
            {
                return true;
            }
        }

        return false;
    }
    
    
}