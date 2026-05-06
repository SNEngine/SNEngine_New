using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Reflection;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Clean code generator using ICommandCodeGenerator. No switch statements.
/// </summary>
public sealed class ScriptCodeGenerator
{
    private readonly Dictionary<Type, ICommandCodeGenerator> _generators = new();

    /// <summary>
    /// Auto-register all generators marked with [SnCodeGenerator]
    /// </summary>
    public void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<SnCodeGeneratorAttribute>();
            if (attr == null) continue;

            if (Activator.CreateInstance(type) is ICommandCodeGenerator instance)
            {
                _generators[attr.TargetNodeType] = instance;
            }
        }
    }

    public string Generate(ScriptNode script)
    {
        var sceneName = script.SceneName ?? "UnnamedScene";

        var statements = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement("SNEngine.API.SNEngine.LoadEmptyScene();")
        };

        foreach (var cmd in script.Commands)
        {
            if (_generators.TryGetValue(cmd.GetType(), out var generator))
            {
                statements.Add(generator.Generate(cmd));
            }
            else
            {
                statements.Add(SyntaxFactory.ParseStatement($"// No code generator for {cmd.GetType().Name}"));
            }
        }

        var executeMethod = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Execute")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithBody(SyntaxFactory.Block(statements));

        var classDeclaration = SyntaxFactory.ClassDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("SNScript")))))
            .AddMembers(
                SyntaxFactory.ConstructorDeclaration(sceneName)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement($"SceneName = \"{sceneName}\";"))),
                executeMethod
            );

        var cu = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
            }))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classDeclaration));

        return cu.NormalizeWhitespace().ToFullString();
    }
}