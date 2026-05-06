using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Reflection;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Clean code generator. Supports functions + ICommandCodeGenerator
/// </summary>
public sealed class ScriptCodeGenerator
{
    private readonly Dictionary<Type, ICommandCodeGenerator> _generators = new();

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

        var members = new List<MemberDeclarationSyntax>();

        // Constructor
        members.Add(SyntaxFactory.ConstructorDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement($"SceneName = \"{sceneName}\";"))));

        // === Main Execute() ===
        var mainStatements = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement("SNEngine.API.SNEngine.LoadEmptyScene();")
        };

        foreach (var cmd in script.Commands)
        {
            mainStatements.Add(GenerateCommand(cmd));
        }

        members.Add(SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Execute")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(mainStatements)));

        // === User functions ===
        foreach (var func in script.Functions)
        {
            var funcStatements = func.Body.Select(GenerateCommand).ToList();

            members.Add(SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                func.Name)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList())
                .WithBody(SyntaxFactory.Block(funcStatements)));
        }

        // Final class
        var classDeclaration = SyntaxFactory.ClassDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("SNScript")))))
            .AddMembers(members.ToArray());

        var cu = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
            }))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classDeclaration));

        return cu.NormalizeWhitespace().ToFullString();
    }

    private StatementSyntax GenerateCommand(CommandNode cmd)
    {
        if (_generators.TryGetValue(cmd.GetType(), out var generator))
        {
            return generator.Generate(cmd);
        }
        return SyntaxFactory.ParseStatement($"// No generator for {cmd.GetType().Name}");
    }
}