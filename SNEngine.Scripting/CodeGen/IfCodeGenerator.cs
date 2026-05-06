using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Full-featured If / ElseIf / Else generator.
/// Supports ANY nested commands using the global generator system + reflection fallback.
/// </summary>
[SnCodeGenerator(typeof(IfCommandNode))]
public sealed class IfCodeGenerator : ICommandCodeGenerator
{
    private readonly IReadOnlyDictionary<Type, ICommandCodeGenerator>? _generators;

    /// <summary>
    /// Default constructor for Activator.CreateInstance
    /// </summary>
    public IfCodeGenerator()
    {
        _generators = null;
    }

    /// <summary>
    /// Constructor with generators dictionary (preferred)
    /// </summary>
    public IfCodeGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
    {
        _generators = generators;
    }

    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not IfCommandNode ifNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid IfCommandNode");

        // Then branch
        var thenBlock = GenerateBlock(ifNode.ThenBody);
        var currentIf = SyntaxFactory.IfStatement(
            SyntaxFactory.ParseExpression(ProcessCondition(ifNode.Condition)),
            thenBlock);

        var lastIf = currentIf;

        // ElseIf branches
        foreach (var elseIf in ifNode.ElseIfClauses)
        {
            var elseIfBlock = GenerateBlock(elseIf.Body);
            var elseIfStmt = SyntaxFactory.IfStatement(
                SyntaxFactory.ParseExpression(ProcessCondition(elseIf.Condition)),
                elseIfBlock);

            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseIfStmt));
        }

        // Else branch
        if (ifNode.ElseBody.Count > 0)
        {
            var elseBlock = GenerateBlock(ifNode.ElseBody);
            lastIf = lastIf.WithElse(SyntaxFactory.ElseClause(elseBlock));
        }

        return lastIf.NormalizeWhitespace();
    }

    /// <summary>
    /// Генерирует Block из списка команд с использованием всех зарегистрированных генераторов
    /// </summary>
    private BlockSyntax GenerateBlock(IEnumerable<CommandNode> commands)
    {
        var statements = commands
            .Select(cmd => GenerateSingleCommand(cmd))
            .ToArray();

        return SyntaxFactory.Block(statements);
    }

    /// <summary>
    /// Главная логика: пытается использовать переданный словарь, потом рефлексию
    /// </summary>
    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd == null)
            return SyntaxFactory.ParseStatement("// Null command inside If");

        // 1. Попытка через переданный словарь
        if (_generators != null && _generators.TryGetValue(cmd.GetType(), out var generator))
        {
            return SafeGenerate(generator, cmd);
        }

        // 2. Fallback через рефлексию
        var fallbackGenerator = FindGeneratorByReflection(cmd.GetType());
        if (fallbackGenerator != null)
        {
            return SafeGenerate(fallbackGenerator, cmd);
        }

        return SyntaxFactory.ParseStatement($"// TODO: Unsupported command inside If: {cmd.GetType().Name}");
    }

    private static StatementSyntax SafeGenerate(ICommandCodeGenerator gen, CommandNode cmd)
    {
        try
        {
            return gen.Generate(cmd);
        }
        catch (Exception ex)
        {
            return SyntaxFactory.ParseStatement($"// ERROR generating {cmd.GetType().Name} inside If: {ex.Message}");
        }
    }

    /// <summary>
    /// Поиск генератора через рефлексию (запасной вариант)
    /// </summary>
    private static ICommandCodeGenerator? FindGeneratorByReflection(Type commandType)
    {
        try
        {
            var generatorType = typeof(SnCodeGeneratorAttribute)
                .Assembly
                .GetTypes()
                .FirstOrDefault(t =>
                {
                    var attr = t.GetCustomAttribute<SnCodeGeneratorAttribute>();
                    return attr?.TargetNodeType == commandType;
                });

            return generatorType != null
                ? Activator.CreateInstance(generatorType) as ICommandCodeGenerator
                : null;
        }
        catch
        {
            return null;
        }
    }

    // ===================================================================
    // ==================== CONDITION PROCESSING =========================
    // ===================================================================

    private string ProcessCondition(string condition)
    {
        condition = condition.Trim();

        var regex = new Regex(@"""[^""]*""|(\b[a-zA-Z_][a-zA-Z0-9_]*\b)");

        return regex.Replace(condition, match =>
        {
            if (match.Value.StartsWith("\""))
                return match.Value;

            string word = match.Value;

            if (double.TryParse(word, out _) ||
                word.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("false", StringComparison.OrdinalIgnoreCase))
                return word;

            if (IsBooleanVariable(word))
                return $"GetVar(\"{word}\").AsBool()";

            if (IsStringVariable(word))
                return $"GetVar(\"{word}\").AsString()";

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
}