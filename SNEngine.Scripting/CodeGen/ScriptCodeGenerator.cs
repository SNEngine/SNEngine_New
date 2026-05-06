using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Генерирует C# код из AST
/// </summary>
public sealed class ScriptCodeGenerator
{
    public string Generate(ScriptNode script)
    {
        var sceneName = script.SceneName ?? "UnnamedScene";

        var statements = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement($"var scene = new {sceneName}Scene();"),
            SyntaxFactory.ParseStatement("context.Engine.SceneManager.LoadScene(scene);")
        };

        foreach (var cmd in script.Commands)
        {
            statements.Add(cmd switch
            {
                ShowBackgroundCommandNode bg =>
                    SyntaxFactory.ParseStatement($"BackgroundAPI.Show(\"{bg.BackgroundName}\");"),

                ShowCharacterCommandNode ch =>
                    SyntaxFactory.ParseStatement($"CharacterAPI.Show(\"{ch.CharacterName}\", \"{ch.Emotion}\");"),

                _ => SyntaxFactory.ParseStatement($"// Unknown command: {cmd.GetType().Name}")
            });
        }

        var method = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Execute")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                    .WithType(SyntaxFactory.ParseTypeName("SNEngine.Scripting.ScriptContext")))))
            .WithBody(SyntaxFactory.Block(statements));

        var cu = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.Core.Scenes")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.Scripting")),
            }))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                SyntaxFactory.ClassDeclaration(sceneName + "Scene")
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .AddMembers(method)));

        return cu.NormalizeWhitespace().ToFullString();
    }
}