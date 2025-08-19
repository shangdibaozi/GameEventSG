using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace GameEventGenerator
{
    public class EventSyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> Methods { get; } = new List<MethodDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Collect all method declarations with attributes
            if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.AttributeLists.Count > 0)
            {
                Methods.Add(methodDeclaration);
            }
        }
    }
}
