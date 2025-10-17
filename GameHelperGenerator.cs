using Microsoft.CodeAnalysis;

namespace GameHelperGenerator
{
    [Generator]
    public class GameEventGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            StaticMethodGenerator.Initialize(context);
            DBGenerator.Initialize(context);
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            StaticMethodGenerator.Execute(context);
            DBGenerator.Execute(context);
        }
    }
}
