using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Универсальный генератор команд (с рефлексией)
/// </summary>
public static class CommandStatementGenerator
{
    public static StatementSyntax Generate(CommandNode cmd, IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
    {
        if (generators.TryGetValue(cmd.GetType(), out var generator))
        {
            return generator.Generate(cmd);
        }

        return SyntaxFactory.ParseStatement($"// No generator registered for {cmd.GetType().Name}");
    }
}