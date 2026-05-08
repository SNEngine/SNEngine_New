using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Generates private void FunctionName() { ... } with logging
/// </summary>
public class FunctionMethodGenerator : BaseCodeGenerator
{
    public FunctionMethodGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators) { }

    public MemberDeclarationSyntax Generate(FunctionNode func)
    {
        Console.WriteLine($"[FunctionMethodGenerator] Generating function: {func.Name}() with {func.Body.Count} commands");

        var statements = func.Body.Select(cmd =>
        {
            var stmt = GenerateCommand(cmd);
            Console.WriteLine($"[FunctionMethodGenerator]   → {cmd.GetType().Name}");
            return stmt;
        }).ToList();

        var method = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), func.Name)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(statements));

        Console.WriteLine($"[FunctionMethodGenerator] Finished function {func.Name}()\n");
        return method;
    }
}