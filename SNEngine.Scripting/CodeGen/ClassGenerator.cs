using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen.Generators;
using System.Collections.Generic;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Orchestrates the full class generation with common namespace for all scripts.
/// This fixes cross-scene references like "Jump To scene2".
/// </summary>
public class ClassGenerator : BaseCodeGenerator
{
    private readonly ExecuteMethodGenerator _executeGen;
    private readonly FunctionMethodGenerator _functionGen;

    public ClassGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators)
    {
        _executeGen = new ExecuteMethodGenerator(generators);
        _functionGen = new FunctionMethodGenerator(generators);
    }

    /// <summary>
    /// Generates full compilation unit with namespace
    /// </summary>
    public CompilationUnitSyntax Generate(ScriptNode script)
    {
        var sceneName = script.SceneName ?? "UnnamedScene";

        var members = new List<MemberDeclarationSyntax>
        {
            ConstructorGenerator.Create(sceneName),
            _executeGen.Generate(script.Commands)
        };

        foreach (var func in script.Functions)
        {
            members.Add(_functionGen.Generate(func));
        }

        // Create class
        var classDeclaration = SyntaxFactory.ClassDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("SNScript")))))
            .AddMembers(members.ToArray());

        // Wrap everything in common namespace
        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName("SNEngine.Game.Scripts"))
            .AddMembers(classDeclaration);

        // Full compilation unit with usings
        return SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.Core"))
            }))
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();
    }
}