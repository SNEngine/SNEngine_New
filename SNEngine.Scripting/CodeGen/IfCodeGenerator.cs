using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(IfCommandNode))]
public sealed class IfCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not IfCommandNode ifNode)
            return SyntaxFactory.ParseStatement("// Invalid IfCommandNode");

        var thenBlock = SyntaxFactory.Block(ifNode.ThenBody.Select(GenerateSingleCommand));
        var currentIf = SyntaxFactory.IfStatement(
            SyntaxFactory.ParseExpression(ProcessCondition(ifNode.Condition)),
            thenBlock);

        var lastIf = currentIf;

        foreach (var elseIf in ifNode.ElseIfClauses)
        {
            var elseIfBlock = SyntaxFactory.Block(elseIf.Body.Select(GenerateSingleCommand));
            var elseIfStmt = SyntaxFactory.IfStatement(
                SyntaxFactory.ParseExpression(ProcessCondition(elseIf.Condition)),
                elseIfBlock);

            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseIfStmt));
        }

        if (ifNode.ElseBody.Count > 0)
        {
            var elseBlock = SyntaxFactory.Block(ifNode.ElseBody.Select(GenerateSingleCommand));
            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseBlock));
        }

        return lastIf;
    }

    private string ProcessCondition(string condition)
    {
        condition = condition.Trim();

        var regex = new Regex(@"""[^""]*""|(\b[a-zA-Z_][a-zA-Z0-9_]*\b)");

        return regex.Replace(condition, match =>
        {
            if (match.Value.StartsWith("\"")) return match.Value;

            string word = match.Value;

            if (double.TryParse(word, out _) ||
                word.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("false", StringComparison.OrdinalIgnoreCase))
                return word;

            if (IsBooleanVariable(word)) return $"GetVar(\"{word}\").AsBool()";
            if (IsStringVariable(word)) return $"GetVar(\"{word}\").AsString()";

            return $"GetVar(\"{word}\").AsInt()";
        });
    }

    private static bool IsBooleanVariable(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var lower = name.ToLowerInvariant();
        return lower.StartsWith("is") || lower.StartsWith("has") || lower.StartsWith("can") ||
               lower == "enabled" || lower == "visible" || lower == "alive";
    }

    private static bool IsStringVariable(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var lower = name.ToLowerInvariant();
        return lower.Contains("name") || lower.Contains("text") || lower == "character";
    }

    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd is PrintCommandNode print)
        {
            string processed = ProcessCondition(print.Message);
            return SyntaxFactory.ParseStatement($"Debug.Log({processed});");
        }

        if (cmd is AssignmentCommandNode assign)
        {
            string rightSide = ProcessCondition(assign.ValueExpression);
            return SyntaxFactory.ParseStatement($"SetVar(\"{assign.VariableName}\", {rightSide});");
        }

        return SyntaxFactory.ParseStatement($"// TODO: {cmd.GetType().Name}");
    }
}