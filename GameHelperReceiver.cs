using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GameHelperGenerator
{
    public class GameHelperReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> Methods { get; } = new List<MethodDeclarationSyntax>();
        public List<FieldDeclarationSyntax> Fields { get; } = new List<FieldDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Collect all field declarations with attributes
            if (syntaxNode is FieldDeclarationSyntax fieldDeclaration &&
                fieldDeclaration.AttributeLists.Count > 0)
            {
                Fields.Add(fieldDeclaration);
            }
            else if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.AttributeLists.Count > 0)
            {
                Methods.Add(methodDeclaration);
            }
        }
    }
}