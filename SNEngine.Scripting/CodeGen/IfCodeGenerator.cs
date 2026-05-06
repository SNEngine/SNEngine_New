using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Generates if (...) { ... } else { ... } (рабочая версия)
/// </summary>
[SnCodeGenerator(typeof(IfCommandNode))]
public sealed class IfCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not IfCommandNode ifNode)
            return SyntaxFactory.ParseStatement("// Invalid IfCommandNode");

        var thenStatements = ifNode.ThenBody
            .Select(cmd => GenerateSingleCommand(cmd))
            .ToList();

        var elseStatements = ifNode.ElseBody
            .Select(cmd => GenerateSingleCommand(cmd))
            .ToList();

        var condition = SyntaxFactory.ParseExpression(
            ProcessCondition(ifNode.Condition)
        );

        var ifStatement = SyntaxFactory.IfStatement(
            condition,
            SyntaxFactory.Block(thenStatements),
            ifNode.ElseBody.Count > 0
                ? SyntaxFactory.ElseClause(SyntaxFactory.Block(elseStatements))
                : null
        );

        return ifStatement;
    }

    private string ProcessCondition(string condition)
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
        return regex.Replace(condition, match =>
        {
            string word = match.Value;
            if (double.TryParse(word, out _) ||
                word.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return word;
            }
            return $"GetVar(\"{word}\").AsInt()";
        });
    }

    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd is PrintCommandNode print)
        {
            string msg = print.Message.Trim();
            if (msg.StartsWith("\"") && msg.EndsWith("\""))
            {
                string content = msg.Substring(1, msg.Length - 2);
                return SyntaxFactory.ParseStatement($"Debug.Log(\"{content}\");");
            }
            return SyntaxFactory.ParseStatement($"Debug.Log({msg});");
        }

        if (cmd is AssignmentCommandNode assign)
        {
            return SyntaxFactory.ParseStatement(
                $"SetVar(\"{assign.VariableName}\", {assign.ValueExpression});");
        }

        return SyntaxFactory.ParseStatement($"// TODO: {cmd.GetType().Name}");
    }
}