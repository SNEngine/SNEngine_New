using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Generates constructor for the scene class
/// </summary>
public static class ConstructorGenerator
{
    public static MemberDeclarationSyntax Create(string sceneName)
    {
        return SyntaxFactory.ConstructorDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement($"SceneName = \"{sceneName}\";")));
    }
}