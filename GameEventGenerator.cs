using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameEventGenerator
{
    [Generator]
    public class GameEventGenerator : ISourceGenerator
    {
        private const string attributeText = @"
/// <summary>
/// static event attribute interface.
/// </summary>
public interface IStaticEventAttribute { }

/// <summary>
/// Switch attribute for skill methods.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Method)]
public class SwitchAttribute : System.Attribute, IStaticEventAttribute 
{
    public string Name { get; }

    public SwitchAttribute(string)
    {
        Name = name;
    }
}
";
        
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(i => i.AddSource("IStaticEventAttribute.g.cs", SourceText.From(attributeText, Encoding.UTF8)));
            // Register our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new EventSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Get our registered syntax receiver
            var receiver = context.SyntaxReceiver as EventSyntaxReceiver;
            if (receiver == null)
                return;

            // Get the IEventAttribute symbol
            INamedTypeSymbol eventAttributeInterface = context.Compilation.GetTypeByMetadataName("IStaticEventAttribute");
            if (eventAttributeInterface == null)
                return;

            // We'll collect methods by attribute type
            Dictionary<INamedTypeSymbol, List<IMethodSymbol>> methodsByAttribute = new Dictionary<INamedTypeSymbol, List<IMethodSymbol>>(SymbolEqualityComparer.Default);
            
            // Special handling for SwitchAttribute - group by attribute parameter
            Dictionary<string, List<IMethodSymbol>> switchMethodsByGroup = new Dictionary<string, List<IMethodSymbol>>();

            foreach (MethodDeclarationSyntax method in receiver.Methods)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(method.SyntaxTree);
                IMethodSymbol methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;

                if (methodSymbol != null && methodSymbol.IsStatic)
                {
                    // Find all attributes that implement IEventAttribute
                    foreach (AttributeData attribute in methodSymbol.GetAttributes())
                    {
                        INamedTypeSymbol attributeClass = attribute.AttributeClass;
                        if (attributeClass != null && 
                            attributeClass.AllInterfaces.Contains(eventAttributeInterface))
                        {
                            // Special handling for SwitchAttribute
                            if (attributeClass.Name == "SwitchAttribute")
                            {
                                // Extract the attribute parameter (group name)
                                string groupName = "";
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
                string switchSource = GenerateSwitchSourceCode(switchMethodsByGroup);
                context.AddSource("SwitchEvent.g.cs", SourceText.From(switchSource, Encoding.UTF8));
            }

            // Generate source code for other event types
            foreach (var pair in methodsByAttribute)
            {
                string eventName = GetEventName(pair.Key.Name);
                string source = GenerateSourceCode(eventName, pair.Value);
                context.AddSource($"{eventName}Event.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        private string GetEventName(string attributeName)
        {
            if (attributeName.EndsWith("Attribute"))
                return attributeName.Substring(0, attributeName.Length - "Attribute".Length);
            return attributeName;
        }

        private string GenerateSourceCode(string eventName, List<IMethodSymbol> methods)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"public static class {eventName}Events");
            sb.AppendLine("{");
            sb.AppendLine($"    public static void {eventName}()");
            sb.AppendLine("    {");
            
            foreach (IMethodSymbol method in methods)
            {
                string className = method.ContainingType.ToDisplayString();
                string methodName = method.Name;
                sb.AppendLine($"        {className}.{methodName}();");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateSwitchSourceCode(Dictionary<string, List<IMethodSymbol>> methodsByGroup)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("public static class SwitchEvent");
            sb.AppendLine("{");

            // Generate a separate method for each group
            foreach (var group in methodsByGroup)
            {
                string groupName = group.Key;
                List<IMethodSymbol> methods = group.Value;

                if (methods.Count > 0)
                {
                    IMethodSymbol firstMethod = methods[0];
                    string parameters = GetMethodParameters(firstMethod);
                    string argumentList = GetArgumentList(firstMethod.Parameters);

                    sb.AppendLine($"    public static void {groupName}_Execute(int switchId, {parameters})");
                    sb.AppendLine("    {");
                    sb.AppendLine("        switch (switchId)");
                    sb.AppendLine("        {");

                    // Parse method names to extract switch IDs and generate case statements
                    foreach (IMethodSymbol method in methods)
                    {
                        string methodName = method.Name;
                        string className = method.ContainingType.ToDisplayString();

                        // Extract switch ID from method name (format: MethodName_switchId)
                        int underscoreIndex = methodName.LastIndexOf('_');
                        if (underscoreIndex > 0 && underscoreIndex < methodName.Length - 1)
                        {
                            string switchIdStr = methodName.Substring(underscoreIndex + 1);
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

        private string GetMethodParameters(IMethodSymbol method)
        {
            // Extract parameter types from the method
            var parameters = new List<string>();
            foreach (var parameter in method.Parameters)
            {
                string refModifier = parameter.RefKind == RefKind.Ref ? "ref " : "";
                parameters.Add($"{refModifier}{parameter.Type.ToDisplayString()} {parameter.Name}");
            }
            return string.Join(", ", parameters);
        }

        private string GetArgumentList(IEnumerable<IParameterSymbol> parameters)
        {
            // Generate argument list for method calls
            var arguments = new List<string>();
            foreach (var parameter in parameters)
            {
                string refModifier = parameter.RefKind == RefKind.Ref ? "ref " : "";
                arguments.Add($"{refModifier}{parameter.Name}");
            }
            return string.Join(", ", arguments);
        }
    }
}
