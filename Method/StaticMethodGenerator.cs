using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GameHelperGenerator
{
    public static class StaticMethodGenerator
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
";

        public static void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(i => i.AddSource("IStaticEventAttribute.g.cs", SourceText.From(attributeText, Encoding.UTF8)));
            // Register our custom syntax receiver
            
        }


        public static void Execute(GeneratorExecutionContext context)
        {
            // Get our registered syntax receiver
            var receiver = context.SyntaxReceiver as GameHelperReceiver;
            if (receiver == null)
                return;

            // Get the IEventAttribute symbol
            var eventAttributeInterface = context.Compilation.GetTypeByMetadataName("IStaticEventAttribute");
            if (eventAttributeInterface == null)
                return;

            // We'll collect methods by attribute type
            var methodsByAttribute = new Dictionary<INamedTypeSymbol, List<IMethodSymbol>>(SymbolEqualityComparer.Default);
            
            // Special handling for SwitchAttribute - group by attribute parameter
            var switchMethodsByGroup = new Dictionary<string, List<IMethodSymbol>>();

            foreach (var method in receiver.Methods)
            {
                var model = context.Compilation.GetSemanticModel(method.SyntaxTree);
                var methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;

                if (methodSymbol != null && methodSymbol.IsStatic)
                {
                    // Find all attributes that implement IEventAttribute
                    foreach (var attribute in methodSymbol.GetAttributes())
                    {
                        var attributeClass = attribute.AttributeClass;
                        if (attributeClass != null && 
                            attributeClass.AllInterfaces.Contains(eventAttributeInterface))
                        {
                            // Special handling for SwitchAttribute
                            if (attributeClass.Name == "SwitchAttribute")
                            {
                                // Extract the attribute parameter (group name)
                                var groupName = "";
                                if (attribute.ConstructorArguments.Length > 0)
                                {
                                    groupName = attribute.ConstructorArguments[0].Value?.ToString() ?? "";
                                }
                                
                                if (!switchMethodsByGroup.ContainsKey(groupName))
                                {
                                    switchMethodsByGroup[groupName] = new List<IMethodSymbol>();
                                }
                                switchMethodsByGroup[groupName].Add(methodSymbol);
                            }
                            else
                            {
                                if (!methodsByAttribute.ContainsKey(attributeClass))
                                {
                                    methodsByAttribute[attributeClass] = new List<IMethodSymbol>();
                                }
                                methodsByAttribute[attributeClass].Add(methodSymbol);
                            }
                        }
                    }
                }
            }

            // Generate source code for SwitchAttribute methods
            if (switchMethodsByGroup.Count > 0)
            {
                var switchSource = GenerateSwitchSourceCode(switchMethodsByGroup);
                context.AddSource("SwitchEvent.g.cs", SourceText.From(switchSource, Encoding.UTF8));
            }

            // Generate source code for other event types
            foreach (var pair in methodsByAttribute)
            {
                var eventName = GetEventName(pair.Key.Name);
                var source = GenerateSourceCode(eventName, pair.Value);
                context.AddSource($"{eventName}Event.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GetEventName(string attributeName)
        {
            if (attributeName.EndsWith("Attribute"))
                return attributeName.Substring(0, attributeName.Length - "Attribute".Length);
            return attributeName;
        }

        private static string GenerateSourceCode(string eventName, List<IMethodSymbol> methods)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"public static class {eventName}Events");
            sb.AppendLine("{");
            sb.AppendLine($"    public static void {eventName}()");
            sb.AppendLine("    {");
            
            foreach (var method in methods)
            {
                var className = method.ContainingType.ToDisplayString();
                var methodName = method.Name;
                sb.AppendLine($"        {className}.{methodName}();");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateSwitchSourceCode(Dictionary<string, List<IMethodSymbol>> methodsByGroup)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("public static class SwitchEvent");
            sb.AppendLine("{");

            // Generate a separate method for each group
            foreach (var group in methodsByGroup)
            {
                var groupName = group.Key;
                var methods = group.Value;

                if (methods.Count > 0)
                {
                    var firstMethod = methods[0];
                    var parameters = GetMethodParameters(firstMethod);
                    var argumentList = GetArgumentList(firstMethod.Parameters);

                    sb.AppendLine($"    public static void {groupName}_Execute(int switchId, {parameters})");
                    sb.AppendLine("    {");
                    sb.AppendLine("        switch (switchId)");
                    sb.AppendLine("        {");

                    // Parse method names to extract switch IDs and generate case statements
                    foreach (var method in methods)
                    {
                        var methodName = method.Name;
                        var className = method.ContainingType.ToDisplayString();

                        // Extract switch ID from method name (format: MethodName_switchId)
                        int underscoreIndex = methodName.LastIndexOf('_');
                        if (underscoreIndex > 0 && underscoreIndex < methodName.Length - 1)
                        {
                            var switchIdStr = methodName.Substring(underscoreIndex + 1);
                            if (int.TryParse(switchIdStr, out int switchId))
                            {
                                sb.AppendLine($"            case {switchId}:");
                                sb.AppendLine($"                {className}.{methodName}({argumentList});");
                                sb.AppendLine("                break;");
                            }
                        }
                    }

                    sb.AppendLine("            default:");
                    sb.AppendLine("                throw new ArgumentException($\"Unknown switchId: {switchId}\");");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GetMethodParameters(IMethodSymbol method)
        {
            // Extract parameter types from the method
            var parameters = new List<string>();
            foreach (var parameter in method.Parameters)
            {
                var refModifier = parameter.RefKind == RefKind.Ref ? "ref " : "";
                parameters.Add($"{refModifier}{parameter.Type.ToDisplayString()} {parameter.Name}");
            }
            return string.Join(", ", parameters);
        }

        private static string GetArgumentList(IEnumerable<IParameterSymbol> parameters)
        {
            // Generate argument list for method calls
            var arguments = new List<string>();
            foreach (var parameter in parameters)
            {
                var refModifier = parameter.RefKind == RefKind.Ref ? "ref " : "";
                arguments.Add($"{refModifier}{parameter.Name}");
            }
            return string.Join(", ", arguments);
        }
    }
}