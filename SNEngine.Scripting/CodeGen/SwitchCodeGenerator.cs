using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(SwitchCommandNode))]
public sealed class SwitchCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not SwitchCommandNode sw)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid SwitchCommandNode");

        Console.WriteLine($"[SwitchCodeGenerator] === START SWITCH ===");
        Console.WriteLine($"[SwitchCodeGenerator] Expression: {sw.Expression}");

        ScopeManager.Current.PushScope();

        try
        {
            var sections = new List<SwitchSectionSyntax>();

            foreach (var c in sw.Cases)
            {
                var labels = SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                    SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(c.Value)));

                var bodyStatements = c.Body
                    .Select(GenerateSingleCommand)
                    .ToList();

                bodyStatements.Add(SyntaxFactory.BreakStatement());

                sections.Add(SyntaxFactory.SwitchSection(labels, SyntaxFactory.List(bodyStatements)));
            }

            if (sw.DefaultBody != null && sw.DefaultBody.Count > 0)
            {
                var defaultLabels = SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                    SyntaxFactory.DefaultSwitchLabel());

                var defaultStatements = sw.DefaultBody
                    .Select(GenerateSingleCommand)
                    .ToList();

                defaultStatements.Add(SyntaxFactory.BreakStatement());

                sections.Add(SyntaxFactory.SwitchSection(defaultLabels, SyntaxFactory.List(defaultStatements)));
            }

            var switchExpr = SyntaxFactory.ParseExpression(sw.Expression);
            var switchStmt = SyntaxFactory.SwitchStatement(switchExpr)
                .WithSections(SyntaxFactory.List(sections));

            var result = switchStmt.NormalizeWhitespace();
            Console.WriteLine($"[SwitchCodeGenerator] === END SWITCH ===\n");
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
            return SyntaxFactory.ParseStatement("// Null command inside Switch");

        // === ГЛАВНОЕ: используем центральный реестр, как в If и For ===
        var generator = CodeGeneratorRegistry.GetGenerator(cmd.GetType());
        if (generator != null)
            return SafeGenerate(generator, cmd);

        return SyntaxFactory.ParseStatement($"// TODO: Unsupported command inside Switch: {cmd.GetType().Name}");
    }

    private static StatementSyntax SafeGenerate(ICommandCodeGenerator gen, CommandNode cmd)
    {
        try
        {
            return gen.Generate(cmd);
        }
        catch (Exception ex)
        {
            return SyntaxFactory.ParseStatement($"// ERROR generating {cmd.GetType().Name} inside Switch: {ex.Message}");
        }
    }
}