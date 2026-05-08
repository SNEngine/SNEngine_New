using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(WhileCommandNode))]
public sealed class WhileCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not WhileCommandNode whileNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid WhileCommandNode");

        Console.WriteLine($"[WhileCodeGenerator] === START WHILE ===");
        Console.WriteLine($"[WhileCodeGenerator] Condition: {whileNode.Condition}");

        ScopeManager.Current.PushScope();

        try
        {
            var bodyStatements = whileNode.Body
                .Select(GenerateSingleCommand)
                .ToList();

            var bodyBlock = SyntaxFactory.Block(bodyStatements);

            var whileStmt = SyntaxFactory.WhileStatement(
                SyntaxFactory.ParseExpression(whileNode.Condition),
                bodyBlock);

            var result = whileStmt.NormalizeWhitespace();
            Console.WriteLine($"[WhileCodeGenerator] === END WHILE ===\n");
            return result;
        }
        finally
        {
            ScopeManager.Current.PopScope();
        }
    }

    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd == null)
            return SyntaxFactory.ParseStatement("// Null command inside While");

        var generator = CodeGeneratorRegistry.GetGenerator(cmd.GetType());
        if (generator != null)
            return SafeGenerate(generator, cmd);

        return SyntaxFactory.ParseStatement($"// TODO: Unsupported command inside While: {cmd.GetType().Name}");
    }

    private static StatementSyntax SafeGenerate(ICommandCodeGenerator gen, CommandNode cmd)
    {
        try
        {
            return gen.Generate(cmd);
        }
        catch (Exception ex)
        {
            return SyntaxFactory.ParseStatement($"// ERROR generating {cmd.GetType().Name} inside While: {ex.Message}");
        }
    }
}