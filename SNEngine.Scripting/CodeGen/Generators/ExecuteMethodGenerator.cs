using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Generates the main Execute() method with logging
/// </summary>
public class ExecuteMethodGenerator : BaseCodeGenerator
{
    public ExecuteMethodGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators) { }

    public MemberDeclarationSyntax Generate(IReadOnlyList<CommandNode> commands)
    {
        Console.WriteLine($"[ExecuteMethodGenerator] Generating Execute() with {commands.Count} top-level commands");

        var statements = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement("SNEngine.API.SNEngine.LoadEmptyScene();")
        };

        foreach (var cmd in commands)
        {
            Console.WriteLine($"[ExecuteMethodGenerator]   Processing: {cmd.GetType().Name}");
            var stmt = GenerateCommand(cmd);
            statements.Add(stmt);
        }

        var method = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Execute")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(statements));

        Console.WriteLine($"[ExecuteMethodGenerator] Finished Execute() — {statements.Count} statements\n");
        return method;
    }
}