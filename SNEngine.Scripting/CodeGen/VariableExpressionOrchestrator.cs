using System;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.API;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Central orchestrator for all variable and expression processing.
/// Single source of truth. No GetGlobal anymore.
/// </summary>
public static class VariableExpressionOrchestrator
{
    /// <summary>
    /// Главный метод: превращает строку из .sn в Roslyn ExpressionSyntax
    /// </summary>
    public static ExpressionSyntax GetExpression(string rawExpr, ScopeManager scope)
    {
        if (string.IsNullOrWhiteSpace(rawExpr))
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        string expr = rawExpr.Trim();

        // Простые литералы
        if (TryParseLiteral(expr, out var literal))
            return literal;

        // Простая переменная
        if (IsSimpleIdentifier(expr))
        {
            string name = expr;

            if (scope.IsLocal(name))
                return SyntaxFactory.IdentifierName(name);

            // Поле класса (создано в ClassGenerator)
            return SyntaxFactory.IdentifierName(name);
        }

        // Сложные выражения ("text " + var, playerHealth < 50 и т.д.)
        return SyntaxFactory.ParseExpression(expr);
    }

    /// <summary>
    /// Для обычного присваивания (x = 10) — поле класса
    /// </summary>
    public static StatementSyntax CreateAssignment(AssignmentCommandNode node, ScopeManager scope)
    {
        string varName = node.VariableName.Trim();
        ExpressionSyntax right = GetExpression(node.ValueExpression, scope);

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(varName),
                right));
    }

    /// <summary>
    /// Для local переменных внутри функций/циклов
    /// </summary>
    public static StatementSyntax CreateLocalAssignment(LocalAssignmentCommandNode node, ScopeManager scope)
    {
        string varName = node.VariableName.Trim();
        ExpressionSyntax right = GetExpression(node.ValueExpression, scope);

        if (!scope.IsLocal(varName))
        {
            scope.Declare(varName, SymbolKind.Variable);

            string typeName = GetTypeForValue(node.ValueExpression);

            var declaration = SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(typeName),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(varName),
                        null,
                        SyntaxFactory.EqualsValueClause(right))));

            return SyntaxFactory.LocalDeclarationStatement(declaration);
        }

        // Если уже объявлена — обычное присваивание
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

        if (double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
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
        !expr.Contains("=") &&
        !expr.Contains("*") &&
        !expr.Contains("/");

    /// <summary>
    /// Возвращает C# тип для значения (используется ClassGenerator и LocalAssignment)
    /// </summary>
    public static string GetTypeForValue(string valueExpression)
    {
        if (string.IsNullOrWhiteSpace(valueExpression))
            return "var";

        string expr = valueExpression.Trim();

        // Сначала double (важно для 3.16, 0.5 и т.д.)
        if (double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            return "double";

        object? sample = TryParseValue(expr);
        var tempVar = new SNVariable(sample ?? 0);
        return tempVar.GetTypeForCompile();
    }

    private static object? TryParseValue(string expr)
    {
        expr = expr.Trim();

        if (int.TryParse(expr, out int i)) return i;
        if (double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) return d;
        if (bool.TryParse(expr, out bool b)) return b;
        if (expr.StartsWith("\"") && expr.EndsWith("\"")) return expr.Trim('"');

        return expr;
    }
}