using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using System.Linq;                    // ← Добавлено

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(PrintCommandNode))]
public sealed class PrintCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not PrintCommandNode print)
            return SyntaxFactory.ParseStatement("// Invalid PrintCommandNode");

        string msg = print.Message.Trim();

        // Простая строка в кавычках
        if (msg.StartsWith("\"") && msg.EndsWith("\"") && msg.Count(f => f == '"') == 2)
        {
            string inner = msg.Substring(1, msg.Length - 2);
            return SyntaxFactory.ParseStatement($"Debug.Log(\"{inner}\");");
        }

        // Всё остальное — через новый оркестратор
        ExpressionSyntax expr = VariableExpressionOrchestrator.GetExpression(msg, ScopeManager.Current);

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("Debug.Log"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(expr)))));
    }
}