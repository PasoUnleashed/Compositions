using System;
using System.Collections.Generic;

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
}