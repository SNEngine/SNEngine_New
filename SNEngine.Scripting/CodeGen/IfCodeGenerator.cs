using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Full-featured If / ElseIf / Else generator with new orchestrator support.
/// </summary>
[SnCodeGenerator(typeof(IfCommandNode))]
public sealed class IfCodeGenerator : ICommandCodeGenerator
{
    private readonly IReadOnlyDictionary<Type, ICommandCodeGenerator>? _generators;

    public IfCodeGenerator() { }
    public IfCodeGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
    {
        _generators = generators;
    }

    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not IfCommandNode ifNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid IfCommandNode");

        // Then branch
        var thenBlock = GenerateBlock(ifNode.ThenBody);
        var currentIf = SyntaxFactory.IfStatement(
            SyntaxFactory.ParseExpression(ProcessCondition(ifNode.Condition)),
            thenBlock);

        var lastIf = currentIf;

        // ElseIf branches
        foreach (var elseIf in ifNode.ElseIfClauses)
        {
            var elseIfBlock = GenerateBlock(elseIf.Body);
            var elseIfStmt = SyntaxFactory.IfStatement(
                SyntaxFactory.ParseExpression(ProcessCondition(elseIf.Condition)),
                elseIfBlock);

            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseIfStmt));
        }

        // Else branch
        if (ifNode.ElseBody.Count > 0)
        {
            var elseBlock = GenerateBlock(ifNode.ElseBody);
            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseBlock));
        }

        return lastIf.NormalizeWhitespace();
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

        if (_generators?.TryGetValue(cmd.GetType(), out var generator) == true)
            return SafeGenerate(generator, cmd);

        var fallbackGenerator = FindGeneratorByReflection(cmd.GetType());
        if (fallbackGenerator != null)
            return SafeGenerate(fallbackGenerator, cmd);

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

    private static ICommandCodeGenerator? FindGeneratorByReflection(Type commandType)
    {
        try
        {
            var generatorType = typeof(SnCodeGeneratorAttribute)
                .Assembly
                .GetTypes()
                .FirstOrDefault(t =>
                {
                    var attr = t.GetCustomAttribute<SnCodeGeneratorAttribute>();
                    return attr?.TargetNodeType == commandType;
                });

            return generatorType != null
                ? Activator.CreateInstance(generatorType) as ICommandCodeGenerator
                : null;
        }
        catch
        {
            return null;
        }
    }

    // ===================================================================
    // ==================== CONDITION PROCESSING =========================
    // ===================================================================

    private string ProcessCondition(string condition)
    {
        // Используем новый оркестратор
        ExpressionSyntax expr = VariableExpressionOrchestrator.GetExpression(condition, ScopeManager.Current);
        return expr.ToFullString();   // временно (в будущем перейдём на полноценный ExpressionSyntax)
    }
}