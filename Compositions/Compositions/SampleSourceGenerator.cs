using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Compositions.Data;
using Compositions.Generators;
using Compositions.Providers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Compositions;

[Generator]
public class SampleSourceGenerator : ISourceGenerator
{
    public static readonly DiagnosticDescriptor NotPartialException = new DiagnosticDescriptor("CST001",
        "Class Must Be Partial", "Found error {0}", "Source Generator", DiagnosticSeverity.Error, true);

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        List<IData> allData = new List<IData>();
        try
        {
            var componentProvider = new ComponentDataProvider();
            var namedTypeSymbols = context.Compilation.GetSymbolsWithName((a) => true, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>();
            foreach (var type in namedTypeSymbols)
            {
                if (type.IsComponent())
                {
                    //EnforcePartial(context,type);
                    allData.AddRange(componentProvider.Provide(type, context));
                }
            }

            foreach (var type in namedTypeSymbols)
            {
                if (type.IsComposition(allData))
                {
                    //EnforcePartial(context, type);
                    allData.Add(new CompositionData(type));
                }
            }


            foreach (var data in allData)
            {
                if (data is CompositionData cmp)
                {
                    foreach (var interfaces in cmp.Type.AllInterfaces)
                    {
                        var cmpForInterface =
                            allData.FirstOrDefault(i => i is CompositionData d && d.Type.Equals(interfaces));
                        if (cmpForInterface != null)
                        {
                            cmp.InheritsComposition.Add(cmpForInterface as CompositionData);
                        }
                    }

                    var current = cmp.Type.BaseType;
                    while (current != null)
                    {
                        var cmpForType =
                            allData.FirstOrDefault(i => i is CompositionData d && d.Type.Equals(current));
                        if (cmpForType != null)
                        {
                            cmp.InheritsComposition.Add(cmpForType as CompositionData);
                        }

                        current = current.BaseType;
                    }

                    foreach (var componentData in allData.OfType<ComponentData>())
                    {
                        if (componentData.PartOfCompositions.Contains(cmp.Type))
                        {
                            cmp.Components.Add(componentData);
                        }

                        var syntax = (cmp.Type.DeclaringSyntaxReferences.First().GetSyntax() as ClassDeclarationSyntax);
                        if (syntax?.InheritsInterface(componentData.GetInterfaceName()) ?? false)
                        {
                            cmp.Components.Add(componentData);
                            continue;
                        }
                    }
                }
            }


            foreach (var data in allData)
            {
                DebugTools.Log(data.ToString());
            }

            List<IGenerator> generators = new List<IGenerator>()
            {
                new CompositionComponentAPIGenerator(),
                new ComponentInterfaceGenerator(),
                new ComponentResetGenerator(),
                new CompositionParentGenerator(),
                new CompositionDisposalGenerator()
            };
            foreach (var generator in generators)
            {
                generator.Generate(context, allData);
            }

            DebugTools.Log("no errors");
        }
        catch (Exception e)
        {
            DebugTools.Log("Errors occured");
            DebugTools.Log(e.ToString());
        }

        context.AddSource("errors.g.cs", $"/*{DebugTools.Flush()}*/");
    }

    public static void EnforcePartial(GeneratorExecutionContext context, INamedTypeSymbol type)
    {
        if (IsPartialClass(type))
        {
            return;
        }
        var typeDeclaringSyntaxReferences = type.DeclaringSyntaxReferences;
        foreach (var declaringSyntaxReference in typeDeclaringSyntaxReferences)
        {
            var diag = Diagnostic.Create(NotPartialException,
                declaringSyntaxReference.GetSyntax().GetLocation(), $"Class {type.Name} must be partial");
            context.ReportDiagnostic(diag);
        }
    }

    public static bool IsPartialClass(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        var typeDeclaringSyntaxReferences = type.DeclaringSyntaxReferences;

        if (!typeDeclaringSyntaxReferences.All(i =>
                (i.GetSyntax() as BaseTypeDeclarationSyntax).Modifiers.Any(j =>
                    j.IsKind(SyntaxKind.PartialKeyword))))
        {
            return false;
        }

        return true;
    }
}