using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(PrintCommandNode))]
public sealed class PrintCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not PrintCommandNode print)
            return SyntaxFactory.ParseStatement("// Invalid PrintCommandNode");

        string msg = print.Message.Trim();

        if (msg.StartsWith("\"") && msg.EndsWith("\"") && msg.Count(f => f == '\"') == 2)
        {
            string inner = msg.Substring(1, msg.Length - 2);
            return SyntaxFactory.ParseStatement($"Debug.Log(\"{inner}\");");
        }

        string processed = ProcessExpression(msg);

        return SyntaxFactory.ParseStatement($"Debug.Log({processed});");
    }

    private static string ProcessExpression(string expr)
    {
        var regex = new Regex(@"""[^""]*""|(\b[a-zA-Z_][a-zA-Z0-9_]*\b)");

        return regex.Replace(expr, match =>
        {
            if (match.Value.StartsWith("\""))
            {
                return match.Value;
            }

            string word = match.Value;

            if (double.TryParse(word, out _) ||
                word.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return word;
            }

            return $"GetVar(\"{word}\")";
        });
    }
}