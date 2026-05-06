using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Smart assignment generator with variable detection in expressions
/// </summary>
[SnCodeGenerator(typeof(AssignmentCommandNode))]
public sealed class AssignmentCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not AssignmentCommandNode assign)
            return SyntaxFactory.ParseStatement("// Invalid AssignmentCommandNode");

        string rightSide = ProcessRightSide(assign.ValueExpression);

        return SyntaxFactory.ParseStatement(
            $"SetVar(\"{assign.VariableName}\", {rightSide});");
    }

    /// <summary>
    /// Обрабатывает правую часть: оборачивает имена переменных в GetVar()
    /// </summary>
    private static string ProcessRightSide(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return "0";

        expression = expression.Trim();

        // Если это чистый литерал — возвращаем как есть
        if (IsLiteral(expression))
            return expression;

        // Ищем имена переменных и оборачиваем их в GetVar("...")
        // Имя переменной: буква + буквы/цифры/подчёркивания
        var regex = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");

        return regex.Replace(expression, match =>
        {
            string word = match.Value;

            // Если это литерал (число, true, false, null) — не трогаем
            if (IsLiteral(word))
                return word;

            // Иначе считаем это переменной
            return $"GetVar(\"{word}\")";
        });
    }

    private static bool IsLiteral(string value)
    {
        if (double.TryParse(value, out _)) return true;
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.StartsWith("\"") || value.StartsWith("'")) return true;
        return false;
    }
}