using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Compositions.Generators;
using Microsoft.CodeAnalysis;

namespace Compositions.Data;

public class CompositionDisposalGenerator : IGenerator
{
    public const string DisposeComponentsOnlyTemplate = @"
namespace {namespace}{
    public partial class {className} {
        public override void Dispose(){
            base.Dispose();
            {disposal}
        }
    }
}";
    public const string RootDisposeTemplate = @"
namespace {namespace}{
    public partial class {className} {
        public virtual void Dispose(){
            foreach(var i in _children){
                try{
                    i.Dispose();
                }
                catch(Exception e){

                }
            }
            {disposal}
            
        }
    }
}";
    public void Generate(GeneratorExecutionContext context, List<IData> datas)
    {
        foreach (var data in datas.OfType<CompositionData>())
        {
            var t = RootDisposeTemplate;
            if (data.InheritsComposition.Any(i => i.Type.TypeKind == TypeKind.Class))
            {
                t = DisposeComponentsOnlyTemplate;
                if (data.GetComponentsToDirectlyImplement().Count() == 0)
                {
                    continue;
                }
                SampleSourceGenerator.EnforcePartial(context,data.Type);
            }

            var dispose = data.GetComponentsToDirectlyImplement()
                .Where(i => i.Symbol.AllInterfaces.Any(j => j.IsOfType("IDisposable", "System")) && i.Members.Count > 0)
                .Select(i => $"this.{i.Symbol.Name}?.Dispose();").Separated("\n");
            context.AddSource($"{data.Type.Name}DisposeAPI.g.cs",
                t.Replace("{namespace}", data.Type.ContainingNamespace.ToString())
                    .Replace("{className}", data.Type.Name).Replace("{disposal}", dispose));
        }
    }
}



public class CompositionParentGenerator : IGenerator
{
    public const string Template = @"
namespace {namespace}{
    public partial class {className} {
        private IComposition _parent;
        public IComposition Parent{get=>_parent;set {
            _parent?.RemoveChild(this);
            _parent = value;
            _parent?.AddChild(this);
        }
        }
        public T CreateChild<T>() where T : IComposition,new(){
            var ret = new T();
            ret.Parent = this;
            return ret;
        }  
        private System.Collections.Generic.HashSet<IComposition> _children = new();
        public void AddChild(IComposition composition){
            _children.Add(composition);
            if(composition.Parent!=this){
                composition.Parent= this;
            }
        }
        public void RemoveChild(IComposition composition){
               _children.Remove(composition);
         }
        
    }
}

";

    public void Generate(GeneratorExecutionContext context, List<IData> datas)
    {
        foreach (var data in datas.OfType<CompositionData>())
        {
            if (data.InheritsComposition.Any(i => i.Type.TypeKind == TypeKind.Class))
            {
                continue;
            }

            SampleSourceGenerator.EnforcePartial(context,data.Type);
            var dispose = data.GetComponentsToDirectlyImplement()
                .Where(i => i.Symbol.AllInterfaces.Any(j => j.IsOfType("IDisposable", "System")) && i.Members.Count > 0)
                .Select(i => $"this.{i.Symbol.Name}?.Dispose();").Separated("\n");
            context.AddSource($"{data.Type.Name}CompositionAPI.g.cs",
                Template.Replace("{namespace}", data.Type.ContainingNamespace.ToString())
                    .Replace("{className}", data.Type.Name).Replace("{disposal}", dispose));
        }
    }
}