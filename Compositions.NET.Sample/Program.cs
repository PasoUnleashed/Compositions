namespace Compositions.NET.Sample
{
    public class Program
    {
        static void Main(string[] args)
        {
        } 
    }

    public partial class ActivationTask : IComposition
    {
        
    }
    public partial class InitialInstanceComponent: IComponent, IClearable
    {
        public float X;
    }

   
}