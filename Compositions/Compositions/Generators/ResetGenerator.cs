using System.Collections.Generic;
using System.Linq;
using System.Text;
using Compositions.Data;
using Microsoft.CodeAnalysis;

namespace Compositions.Generators;

public class ComponentResetGenerator : IGenerator
{
    public const string Template = @"
namespace {namespace}{
    public partial class {className} {
        public void Clear(){
            {clearers};
        }
    }        
}
";
    public void Generate(GeneratorExecutionContext context, List<IData> datas)
    {
        foreach (var componentData in datas.OfType<ComponentData>())
        {
            bool isClearable = componentData.Symbol.InheritsInterface("IClearable", "Composition");
            if (isClearable)
            {
                SampleSourceGenerator.EnforcePartial(context,componentData.Symbol);
                
            }
            else
            {
                continue;
            }

            string clearers = componentData.Members.Select(i => $"this.{i.Name}=default;").Separated(sep:"\n");
            var gen = Template
                .Replace("{className}", componentData.Symbol.Name)
                .Replace("{clearers}",
                    clearers)
                .Replace("{namespace}",
                    componentData.Symbol.ContainingNamespace.ToString());
            context.AddSource($"{componentData.Symbol.Name}Clearer.g.cs", gen);
        }
    }
}
public class ComponentInterfaceGenerator : IGenerator
{
    private const string TemplateMemberOwner = @"
public interface I{componentName}ComponentHaver : Compositions.IComposition{
    public void Set{componentName}({args});
    public void Remove{componentName}();
    public bool Has{componentName}{get;}
    public {componentClass} {componentName}{get;}
}

    ";
    private const string TemplateFlag= @"
public interface I{componentName}ComponentHaver{
    public bool Is{componentName}{get;set;}
}

    ";
    public void Generate(GeneratorExecutionContext context, List<IData> datas)
    {

        foreach (var componentData in datas.OfType<ComponentData>())
        {
            var t = TemplateMemberOwner;
            if (componentData.Members.Count == 0)
            {
                t = TemplateFlag;
            }
            var gen = t
                .Replace("{componentClass}", componentData.Symbol.ToString())
                .Replace("{args}",
                    componentData.Members.Select(i => $"{i.Type.ToString()} {i.Name.ToLower()}").Separated())
                .Replace("{componentSetters}",
                    componentData.Members.Select(i => $"{i.Name}={i.Name.ToLower()}").Separated())
                .Replace("{currentSetters}",
                    componentData.Members.Select(i => $"this.{{componentName}}.{i.Name} = {i.Name.ToLower()}")
                        .Separated(";\n"))
                    
                .Replace("{componentName}", componentData.GetName());
            context.AddSource($"Interface{componentData.Symbol.Name}.g.cs",gen);
        }
    }
}

public class CompositionComponentAPIGenerator : IGenerator
{
    private const string TemplateMemberOwner = @"
namespace {namespace}{
    public partial class {className} : I{componentName}ComponentHaver {
        public void Set{componentName}({args}){
            var next = Compositions.ComponentPool.Get<{componentClass}>();
            if(this._{componentNameL} != default){
               //use property to return to pool if it originated from pool
               this.{componentName}=default;
            }
            this.{componentName} = next;
            this._{componentNameL}IsInstanceSet = false;    
            {componentSetters};
        }
        public void Remove{componentName}(){
            this.{componentName} = default;
        }
        private {componentClass} _{componentNameL};
        public {componentClass} {componentName} {set=>InstanceSet{componentName}(value);  get {
            if(_{componentNameL} != null){
                return _{componentNameL};
            }
            return Get{componentName}FromParent();
        }
        }
        private bool _{componentNameL}IsInstanceSet =false;
        private void InstanceSet{componentName}({componentClass} {componentNameL}){
            if(!_{componentNameL}IsInstanceSet && this._{componentNameL} != null){
               Compositions.ComponentPool.Return(_{componentNameL});
            }
            this._{componentNameL}IsInstanceSet=true;
            this._{componentNameL} = {componentNameL};
        }
        private {componentClass} Get{componentName}FromParent(){
            var current =Parent;
            while(current!=null){
                if(current is I{componentName}ComponentHaver h && h.Has{componentName}){
                    return h.{componentName};
                }
                current = current.Parent;
            }
            return default;
        }
        public bool Has{componentName}{get=>this.{componentName}!=null;}
    }
}
";

    private const string TemplateFlag = @"
namespace {namespace}{
    public partial class {className} : I{componentName}ComponentHaver{
        private bool _is{componentName};
        public bool Is{componentName} {get=> this._is{componentName} || (Parent is I{componentName}ComponentHaver h && h.Is{componentName});set=>_is{componentName}=value;}
    }
}
";
    private const string NoInheritTemplateMemberOwner = @"
namespace {namespace}{
    public partial class {className} : I{componentName}ComponentHaver {
        public void Set{componentName}({args}){
            var next = Compositions.ComponentPool.Get<{componentClass}>();
            if(this._{componentNameL} != default){
               //use property to return to pool if it originated from pool
               this.{componentName}=default;
            }
            this.{componentName} = next;
            this._{componentNameL}IsInstanceSet = false;    
            {componentSetters};
        }
        public void Remove{componentName}(){
            this.{componentName} = default;
        }
        private {componentClass} _{componentNameL};
        public {componentClass} {componentName} {set=>InstanceSet{componentName}(value);  get {
            if(_{componentNameL} != null){
                return _{componentNameL};
            }
            return default;
        }
        }
        private bool _{componentNameL}IsInstanceSet =false;
        private void InstanceSet{componentName}({componentClass} {componentNameL}){
            if(!_{componentNameL}IsInstanceSet && this._{componentNameL} != null){
               Compositions.ComponentPool.Return(_{componentNameL});
            }
            this._{componentNameL}IsInstanceSet=true;
            this._{componentNameL} = {componentNameL};
        }
        public bool Has{componentName}{get=>this.{componentName}!=null;}
    }
}
";

    private const string NoInheritTemplateFlag = @"
namespace {namespace}{
    public partial class {className} : I{componentName}ComponentHaver{
        private bool _is{componentName};
        public bool Is{componentName} {get=> this._is{componentName};set=>_is{componentName}=value;}
    }
}
";
    public void Generate(GeneratorExecutionContext context, List<IData> datas)
    {
        foreach (var data in datas.OfType<CompositionData>())
        {
            foreach (var componentData in data.GetComponentsToDirectlyImplement().Distinct())
            {
                
                SampleSourceGenerator.EnforcePartial(context,data.Type);
                var t = TemplateMemberOwner;
                bool dontinherit = componentData.Symbol.GetAllAttributes()
                    .Any(i => i.AttributeClass.IsOfType("DontInheritAttribute", "Compositions"));
                
                if (dontinherit)
                {
                    t = NoInheritTemplateMemberOwner;
                }
                if (componentData.Members.Count == 0)
                {
                    if (!dontinherit)
                    {
                        t = TemplateFlag;
                    }
                    else
                    {
                        t = NoInheritTemplateFlag;
                    }
                }

                var gen = t.Replace("{namespace}", data.Type.ContainingNamespace.ToString())
                    .Replace("{className}", data.Type.Name)
                    .Replace("{componentClass}", componentData.Symbol.ToString())
                    .Replace("{args}",
                        componentData.Members.Select(i => $"{i.Type.ToString()} {i.Name.ToLower()}").Separated())
                    .Replace("{componentSetters}",
                        componentData.Members.Select(i => $"this.{{componentName}}.{i.Name}={i.Name.ToLower()}").Separated(sep:";"))
                    .Replace("{currentSetters}",
                        componentData.Members.Select(i => $"this.{{componentName}}.{i.Name} = {i.Name.ToLower()}")
                            .Separated(";\n"))
                    
                    .Replace("{componentName}", componentData.GetName())
                    .Replace("{componentNameL}", componentData.GetName().ToLower());
                context.AddSource($"{data.Type.Name}{componentData.Symbol.Name}API.g.cs",gen);
            }
            
        }
    }
    
}

public interface IGenerator
{
    public void Generate(GeneratorExecutionContext context, List<IData> datas);
}

public static class StringUtils
{
    public static string Separated<T>(this IEnumerable<T> items,string sep = ", ")
    {
        int i = 0;
        StringBuilder sb = new StringBuilder();
        foreach (var item in items)
        {
            if (i > 0)
            {
                sb.Append(sep);
            }

            i += 1;
            sb.Append(item.ToString());

        }

        return sb.ToString();
    }
}