using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Compositions.Data;

public class CompositionData : IData
{
    public INamedTypeSymbol Type;
    public List<ComponentData> Components= new List<ComponentData>();
    public List<CompositionData> InheritsComposition= new List<CompositionData>();

    public IEnumerable<ComponentData> InheritedComponents
    {
        get
        {
           return InheritsComposition.SelectMany(i => i.AllComponents).Distinct();
        }
    }
    public IEnumerable<ComponentData> AllComponents
    {
        get
        {
            return Components.Concat(InheritedComponents).Distinct();
        }
    }

    public CompositionData(INamedTypeSymbol type)
    {
        Type = type;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Type.Name);
        sb.AppendLine(" Declared Components:");
        foreach (var component in Components)
        {
            sb.AppendLine($"     {component.Symbol.Name}");
        }

        sb.AppendLine(" Inherited Components:");
        foreach (var inheritedComponent in InheritedComponents)
        {
            sb.AppendLine($"     {inheritedComponent.Symbol.Name}");
        }

        sb.AppendLine(" Implemented Components:");
        foreach (var componentData in GetComponentsToDirectlyImplement())
        {
            sb.AppendLine($"     {componentData.Symbol.Name}");
        }
        return sb.ToString();
    }
    
    public bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseTypeSymbol)
    {
        // Traverse the base types (ancestor chain)
        INamedTypeSymbol currentBaseType = typeSymbol.BaseType;
    
        while (currentBaseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentBaseType, baseTypeSymbol))
            {
                return true; // Found the base type in the inheritance chain
            }
        
            currentBaseType = currentBaseType.BaseType;
        }

        return false; // Base type not found in the inheritance chain
    }

    public IEnumerable<ComponentData> GetComponentsToDirectlyImplement()
    {
        if (Type.TypeKind != TypeKind.Class)
        {
            yield break;
        }
        foreach (var component in this.AllComponents)
        {
            bool found = false;
            Stack<CompositionData> datas = new Stack<CompositionData>();
            foreach (var data in InheritsComposition)
            {
                datas.Push(data);
            }

            while (datas.Count > 0)
            {
                var current = datas.Pop();
                if (current.GetComponentsToDirectlyImplement().Contains(component))
                {
                    found = true;
                    break;
                }

                foreach (var data in current.InheritsComposition)
                {
                    datas.Push(data);
                }
            }
            if (!found)
            {
                yield return component;
            }
        }
    }
}