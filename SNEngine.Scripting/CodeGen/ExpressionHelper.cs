using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Надёжный хелпер для async. Все вызовы функций выносятся во временные переменные.
/// </summary>
public static class ExpressionHelper
{
    public static (List<StatementSyntax> statements, ExpressionSyntax finalExpr) WrapWithTempIfNeeded(string expr)
    {
        var statements = new List<StatementSyntax>();

        if (string.IsNullOrWhiteSpace(expr))
            return (statements, SyntaxFactory.ParseExpression("0"));

        // Если нет вызова функции — возвращаем как есть
        if (!Regex.IsMatch(expr, @"\b[a-zA-Z_][a-zA-Z0-9_]*\s*\("))
            return (statements, SyntaxFactory.ParseExpression(expr));

        string current = expr.Trim();

        // Обрабатываем от самого глубокого вызова к внешнему
        var matches = Regex.Matches(current, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(");

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var m = matches[i];
            string funcName = m.Groups[1].Value;

            if (funcName == "ToString" || funcName == "GetType" || funcName == "Equals")
                continue;

            // Находим полный вызов функции (учитывая вложенные скобки)
            int start = m.Index;
            int depth = 0;
            int end = start;

            for (int j = start; j < current.Length; j++)
            {
                if (current[j] == '(') depth++;
                if (current[j] == ')') depth--;
                if (depth == 0 && current[j] == ')')
                {
                    end = j + 1;
                    break;
                }
            }

            string fullCall = current.Substring(start, end - start);
            string tempName = "temp_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            statements.Add(SyntaxFactory.ParseStatement($"var {tempName} = await {fullCall};"));

            // Заменяем вызов на tempName
            current = current.Substring(0, start) + tempName + current.Substring(end);
        }

        return (statements, SyntaxFactory.ParseExpression(current));
    }
}