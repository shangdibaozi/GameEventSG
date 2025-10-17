using Microsoft.CodeAnalysis;

namespace GameHelperGenerator
{
    [Generator]
    public class GameHelperGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            StaticMethodGenerator.Initialize(context);
            context.RegisterForSyntaxNotifications(() => new GameHelperReceiver());
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            DBGenerator.Execute(context);
            StaticMethodGenerator.Execute(context);
        }
    }
}
