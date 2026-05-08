using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Full-featured If / ElseIf / Else generator
/// Теперь использует центральный CodeGeneratorRegistry (без дублирования)
/// </summary>
[SnCodeGenerator(typeof(IfCommandNode))]
public sealed class IfCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not IfCommandNode ifNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid IfCommandNode");

        Console.WriteLine($"[IfCodeGenerator] === START IF ===");
        Console.WriteLine($"[IfCodeGenerator] Condition: {ifNode.Condition}");

        var thenBlock = GenerateBlock(ifNode.ThenBody);
        var currentIf = SyntaxFactory.IfStatement(
            SyntaxFactory.ParseExpression(ProcessCondition(ifNode.Condition)),
            thenBlock);

        var lastIf = currentIf;

        // ElseIf branches
        foreach (var elseIf in ifNode.ElseIfClauses)
        {
            Console.WriteLine($"[IfCodeGenerator]   ElseIf: {elseIf.Condition}");
            var elseIfBlock = GenerateBlock(elseIf.Body);
            var elseIfStmt = SyntaxFactory.IfStatement(
                SyntaxFactory.ParseExpression(ProcessCondition(elseIf.Condition)),
                elseIfBlock);

            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseIfStmt));
        }

        // Else branch
        if (ifNode.ElseBody.Count > 0)
        {
            Console.WriteLine($"[IfCodeGenerator]   Else branch present");
            var elseBlock = GenerateBlock(ifNode.ElseBody);
            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseBlock));
        }

        var result = lastIf.NormalizeWhitespace();
        Console.WriteLine($"[IfCodeGenerator] === END IF ===\n");
        return result;
    }

    private BlockSyntax GenerateBlock(IEnumerable<CommandNode> commands)
    {
        var statements = commands
            .Select(GenerateSingleCommand)
            .ToArray();

        return SyntaxFactory.Block(statements);
    }

    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd == null)
            return SyntaxFactory.ParseStatement("// Null command inside If");

        // Используем центральный реестр (без дублирования!)
        var generator = CodeGeneratorRegistry.GetGenerator(cmd.GetType());
        if (generator != null)
            return SafeGenerate(generator, cmd);

        return SyntaxFactory.ParseStatement($"// TODO: Unsupported command inside If: {cmd.GetType().Name}");
    }

    private static StatementSyntax SafeGenerate(ICommandCodeGenerator gen, CommandNode cmd)
    {
        try
        {
            return gen.Generate(cmd);
        }
        catch (Exception ex)
        {
            return SyntaxFactory.ParseStatement($"// ERROR generating {cmd.GetType().Name} inside If: {ex.Message}");
        }
    }

    private string ProcessCondition(string condition)
    {
        Console.WriteLine($"[IfCodeGenerator] Processing condition: {condition}");
        ExpressionSyntax expr = VariableExpressionOrchestrator.GetExpression(condition, ScopeManager.Current);
        string result = expr.ToFullString();
        Console.WriteLine($"[IfCodeGenerator] Condition result: {result}");
        return result;
    }
}