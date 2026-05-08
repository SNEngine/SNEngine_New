using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.API;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Central orchestrator for all variable and expression processing.
/// Single source of truth.
/// </summary>
public static class VariableExpressionOrchestrator
{
    private static readonly Regex IdentifierRegex = new(
        @"(""[^""]*"")|(\b[a-zA-Z_][a-zA-Z0-9_]*\b)", RegexOptions.Compiled);

    /// <summary>
    /// Главный метод: превращает строку из .sn в Roslyn ExpressionSyntax
    /// </summary>
    public static ExpressionSyntax GetExpression(string rawExpr, ScopeManager scope)
    {
        if (string.IsNullOrWhiteSpace(rawExpr))
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        rawExpr = rawExpr.Trim();

        // Простые литералы
        if (TryParseLiteral(rawExpr, out var literal))
            return literal;

        // Простая переменная
        if (IsSimpleIdentifier(rawExpr))
        {
            string name = rawExpr.Trim();
            if (scope.IsLocal(name))
                return SyntaxFactory.IdentifierName(name);

            return CreateGetGlobal(name); // временно
        }

        // Сложное выражение (например: "Iteration: " + i  или playerHealth < 50)
        return SyntaxFactory.ParseExpression(rawExpr); // пока fallback
    }

    /// <summary>
    /// Для Assignment (playerHealth = 35)
    /// </summary>
    public static StatementSyntax CreateAssignment(AssignmentCommandNode node, ScopeManager scope)
    {
        string varName = node.VariableName.Trim();
        ExpressionSyntax right = GetExpression(node.ValueExpression, scope);

        // Просто присваивание (поле уже создано в ClassGenerator)
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(varName),
                right));
    }

    private static bool TryParseLiteral(string expr, out ExpressionSyntax syntax)
    {
        syntax = null!;

        if (int.TryParse(expr, out int i))
        {
            syntax = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i));
            return true;
        }
        if (double.TryParse(expr, out double d))
        {
            syntax = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(d));
            return true;
        }
        if (bool.TryParse(expr, out bool b))
        {
            syntax = b ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                       : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            return true;
        }
        if (expr.StartsWith("\"") && expr.EndsWith("\""))
        {
            syntax = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(expr.Trim('"')));
            return true;
        }
        return false;
    }

    private static bool IsSimpleIdentifier(string expr) =>
        !expr.Contains(" ") && !expr.Contains("+") && !expr.Contains("<") && !expr.Contains(">") && !expr.Contains("=");

    private static string InferType(string value)
    {
        if (int.TryParse(value, out _)) return "int";
        if (double.TryParse(value, out _)) return "double";
        if (bool.TryParse(value, out _)) return "bool";
        if (value.StartsWith("\"")) return "string";
        return "var";
    }

    private static InvocationExpressionSyntax CreateGetGlobal(string name)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("GetGlobal"),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name))))));
    }
}