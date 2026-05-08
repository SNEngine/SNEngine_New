using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.API;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Central orchestrator for all variable and expression processing.
/// Uses SNVariable.GetTypeForCompile() for accurate type inference.
/// No more GetGlobal.
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

            // Это поле класса — просто имя
            return SyntaxFactory.IdentifierName(name);
        }

        // Сложные выражения (конкатенация, условия и т.д.)
        return SyntaxFactory.ParseExpression(rawExpr);
    }

    /// <summary>
    /// Для Assignment — используем SNVariable для определения типа (передаём в ClassGenerator)
    /// </summary>
    public static StatementSyntax CreateAssignment(AssignmentCommandNode node, ScopeManager scope)
    {
        string varName = node.VariableName.Trim();
        ExpressionSyntax right = GetExpression(node.ValueExpression, scope);

        // Просто присваивание — поле уже создано в ClassGenerator
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
            syntax = b
                ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
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
        !expr.Contains(" ") &&
        !expr.Contains("+") &&
        !expr.Contains("<") &&
        !expr.Contains(">") &&
        !expr.Contains("=");

    /// <summary>
    /// Вспомогательный метод — создаёт SNVariable и возвращает тип (можно использовать в ClassGenerator)
    /// </summary>
    public static string GetTypeForValue(string valueExpression)
    {
        if (string.IsNullOrWhiteSpace(valueExpression))
            return "var";

        string expr = valueExpression.Trim();

        // Сначала пробуем double (важно для 3.16)
        if (double.TryParse(expr, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _))
            return "double";

        object? sample = TryParseValue(expr);
        var tempVar = new SNVariable(sample ?? 0);
        return tempVar.GetTypeForCompile();
    }

    private static object? TryParseValue(string expr)
    {
        expr = expr?.Trim() ?? "";

        if (int.TryParse(expr, out int i)) return i;
        if (double.TryParse(expr, out double d)) return d;
        if (bool.TryParse(expr, out bool b)) return b;
        if (expr.StartsWith("\"") && expr.EndsWith("\"")) return expr.Trim('"');

        return expr; // fallback
    }
}