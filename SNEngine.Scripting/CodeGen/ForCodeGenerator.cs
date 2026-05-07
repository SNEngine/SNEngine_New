using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(ForCommandNode))]
public sealed class ForCodeGenerator : ICommandCodeGenerator
{
    private readonly IReadOnlyDictionary<Type, ICommandCodeGenerator>? _generators;

    public ForCodeGenerator() { _generators = null; }

    public ForCodeGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
    {
        _generators = generators;
    }

    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not ForCommandNode forNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid ForCommandNode");

        // Генерируем тело for
        var bodyBlock = GenerateBlock(forNode.Body);

        var forStatement = SyntaxFactory.ForStatement(bodyBlock)
            .WithDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(forNode.Variable))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.ParseExpression(forNode.Init.Split('=')[1].Trim()))))))
            .WithCondition(
                SyntaxFactory.ParseExpression(forNode.Condition.Trim()))
            .WithIncrementors(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.ParseExpression(forNode.Increment.Trim()) as ExpressionSyntax));

        return forStatement.NormalizeWhitespace();
    }

    private BlockSyntax GenerateBlock(IEnumerable<CommandNode> commands)
    {
        var statements = commands
            .Select(cmd => GenerateSingleCommand(cmd))
            .ToArray();

        return SyntaxFactory.Block(statements);
    }

    private StatementSyntax GenerateSingleCommand(CommandNode cmd)
    {
        if (cmd == null)
            return SyntaxFactory.ParseStatement("// Null command inside For");

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

        return SyntaxFactory.ParseStatement($"// TODO: Unsupported command inside For: {cmd.GetType().Name}");
    }

    private static StatementSyntax SafeGenerate(ICommandCodeGenerator gen, CommandNode cmd)
    {
        try
        {
            return gen.Generate(cmd);
        }
        catch (Exception ex)
        {
            return SyntaxFactory.ParseStatement($"// ERROR generating {cmd.GetType().Name} inside For: {ex.Message}");
        }
    }

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
}