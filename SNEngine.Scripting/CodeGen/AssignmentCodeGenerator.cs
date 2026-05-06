using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(AssignmentCommandNode))]
public sealed class AssignmentCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not AssignmentCommandNode assign)
            return SyntaxFactory.ParseStatement("// Invalid AssignmentCommandNode");

        string rightSide = ProcessRightSide(assign.ValueExpression);

        return SyntaxFactory.ParseStatement($"SetVar(\"{assign.VariableName}\", {rightSide});");
    }

    private static string ProcessRightSide(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return "0";
        expr = expr.Trim();

        var regex = new Regex(@"""[^""]*""|(\b[a-zA-Z_][a-zA-Z0-9_]*\b)");

        return regex.Replace(expr, match =>
        {
            if (match.Value.StartsWith("\"")) return match.Value;

            string word = match.Value;

            if (double.TryParse(word, out _) ||
                word.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("false", StringComparison.OrdinalIgnoreCase))
                return word;

            return $"GetVar(\"{word}\")";
        });
    }
}