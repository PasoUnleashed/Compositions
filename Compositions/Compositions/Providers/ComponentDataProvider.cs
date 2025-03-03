using System.Collections.Generic;
using System.Linq;
using Compositions.Data;
using Microsoft.CodeAnalysis;

namespace Compositions.Providers;

public class ComponentDataProvider : IDataProvider
{
    public class SymbolUtils
    {
        public static List<ISymbol> GetPublicSettableInstanceMembers(INamedTypeSymbol typeSymbol)
        {
            // List to store the public settable fields and properties
            var publicSettableMembers = new List<ISymbol>();

            // Get all members of the type
            var members = typeSymbol.GetMembers();

            // Filter for public instance fields
            var publicFields = members.OfType<IFieldSymbol>()
                .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsReadOnly && !f.IsStatic)
                .ToList();
            publicSettableMembers.AddRange(publicFields);

            // Filter for public instance properties with a setter
            var publicProperties = members.OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public 
                            && !p.IsStatic 
                            && p.SetMethod != null) // Must have a setter
                .ToList();
            publicSettableMembers.AddRange(publicProperties);

            return publicSettableMembers;
        }
    }
    public IData[] Provide(INamedTypeSymbol symbol,GeneratorExecutionContext context)
    {
        var ret = new ComponentData(symbol);
        var inter = context.Compilation.GetSymbolsWithName($"I{symbol.Name.Replace("Component","")}ComponentHaver", SymbolFilter.Type)
            .FirstOrDefault();
       
        foreach (var member in SymbolUtils.GetPublicSettableInstanceMembers(symbol))
        {
            if (member is IFieldSymbol f)
            {
                ret.Members.Add(new MemberData(f.Type,f.Name));
            }else if (member is IPropertySymbol p)
            {
                ret.Members.Add(new MemberData(p.Type,p.Name));
            }
        }
        foreach (var inclusion in symbol.GetComponentCompositionInclusions())
        {
            ret.PartOfCompositions.Add(inclusion);
        }

        return new[] { ret };
    }
}

public interface IDataProvider
{
    public IData[] Provide(INamedTypeSymbol symbol,GeneratorExecutionContext context);
}