using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GameHelperGenerator
{
    [Generator]
    public class GameHelperGenerator : ISourceGenerator
    {
        private const string attributeText = @"
/// <summary>
/// static event attribute interface.
/// </summary>
public interface IStaticEventAttribute { }

/// <summary>
/// 捕获静态方法，加入switch语句中。
/// name表示要捕获的方法名称前缀。
/// 被捕获的必须是public静态方法，方法格式必须以：名称_switchId组织
/// eg.public static void MyMethod_1000() { }
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Method)]
public class SwitchAttribute : System.Attribute, IStaticEventAttribute 
{
    public string Name { get; }

    public SwitchAttribute(string name)
    {
        Name = name;
    }
}
/// <summary>
/// 收集带有指定前缀方法添加到""suffixName()""方法中
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
public class MethodCollectorAttribute : System.Attribute
{
    public string SuffixName { get; }

    public MethodCollectorAttribute(string suffixName)
    {
        SuffixName = suffixName;
    }
}
";
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(i => i.AddSource("CustomAttributes.g.cs", SourceText.From(attributeText, Encoding.UTF8)));
            context.RegisterForSyntaxNotifications(() => new GameHelperReceiver());
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as GameHelperReceiver;
            if (receiver == null)
            {
                return;
            }
            DBGenerator.Execute(context, receiver);
            MemberMethodGenerator.Execute(context, receiver);
            StaticMethodGenerator.Execute(context, receiver);
        }
    }
}
