using System;
using System.Collections.Generic;
using System.Linq;

namespace Compositions
{
    public interface IComposition : IDisposable
    {
        IComposition Parent { get; set; }
        T CreateChild<T>() where T : IComposition,new();
        void AddChild(IComposition composition);
        void RemoveChild(IComposition composition);
    }

    public interface IComponent
    {
        //void Reset();
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]

    public class IncludedInCompositionAttribute : System.Attribute
    {
        public readonly Type Type;

        public IncludedInCompositionAttribute(Type type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DontInheritAttribute : System.Attribute
    {
        
    }

    public interface IClearable
    {
        void Clear();
    }

    public static class ComponentPool
    {
        private static Dictionary<Type, List<IComponent>> _components = new Dictionary<Type, List< IComponent>>();

        public static T Get<T>() where T : IComponent
        {
            if (!_components.TryGetValue(typeof(T), out var list))
            {
                list = new List<IComponent>();
                _components.Add(typeof(T), list);
            }

            if (list.Count == 0)
            {
                return Activator.CreateInstance<T>();
            }
            var ret = (T)list.Last();
            list.RemoveAt(list.Count - 1);
            return ret;
        }

        public static void Return(IComponent component)
        {
            var type = component.GetType();
            if (!_components.TryGetValue(type, out var list))
            {
                list = new List<IComponent>();
                _components[type] = list;
            }

            if (component is IClearable clearable)
            {
                clearable.Clear();
            }
            list.Add(component);
        }
    }
}