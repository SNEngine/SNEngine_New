using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(IfCommandNode))]
public sealed class IfCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not IfCommandNode ifNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid IfCommandNode");

        var statements = new List<StatementSyntax>();

        var (conditionExpr, preStatements) = ProcessConditionWithTemp(ifNode.Condition);
        statements.AddRange(preStatements);

        var thenBlock = GenerateBlock(ifNode.ThenBody);
        var ifStmt = SyntaxFactory.IfStatement(conditionExpr, thenBlock);

        if (ifNode.ElseBody.Count > 0)
        {
            var elseBlock = GenerateBlock(ifNode.ElseBody);
            ifStmt = ifStmt.WithElse(SyntaxFactory.ElseClause(elseBlock));
        }

        statements.Add(ifStmt);
        return SyntaxFactory.Block(statements);
    }

    private (ExpressionSyntax condition, List<StatementSyntax> pre) ProcessConditionWithTemp(string condition)
    {
        var (stmts, finalExpr) = ExpressionHelper.WrapWithTempIfNeeded(condition);
        return (finalExpr, stmts);
    }

    private BlockSyntax GenerateBlock(IEnumerable<CommandNode> commands)
    {
        var statements = commands.Select(GenerateSingleCommand).ToArray();
        return SyntaxFactory.Block(statements);
    }

    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd == null) return SyntaxFactory.ParseStatement("// Null command inside If");
        var generator = CodeGeneratorRegistry.GetGenerator(cmd.GetType());
        return generator != null ? SafeGenerate(generator, cmd) : SyntaxFactory.ParseStatement($"// TODO: {cmd.GetType().Name}");
    }

    private static StatementSyntax SafeGenerate(ICommandCodeGenerator gen, CommandNode cmd)
    {
        try { return gen.Generate(cmd); }
        catch (Exception ex) { return SyntaxFactory.ParseStatement($"// ERROR: {ex.Message}"); }
    }
}