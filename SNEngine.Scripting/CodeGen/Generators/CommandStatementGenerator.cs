using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Универсальный генератор команд с подробным логированием
/// </summary>
public static class CommandStatementGenerator
{
    public static StatementSyntax Generate(CommandNode cmd, IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
    {
        Console.WriteLine($"[CommandStatementGenerator] Generating command: {cmd.GetType().Name}");

        if (generators.TryGetValue(cmd.GetType(), out var generator))
        {
            var result = generator.Generate(cmd);
            Console.WriteLine($"[CommandStatementGenerator]   → Success using registered generator");
            return result;
        }

        Console.WriteLine($"[CommandStatementGenerator]   ⚠ No generator found for {cmd.GetType().Name}");
        return SyntaxFactory.ParseStatement($"// No generator registered for {cmd.GetType().Name}");
    }
}