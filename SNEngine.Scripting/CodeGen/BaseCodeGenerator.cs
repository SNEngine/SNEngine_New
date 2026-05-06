using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Base class for all code generators
/// </summary>
public abstract class BaseCodeGenerator
{
    protected readonly IReadOnlyDictionary<Type, ICommandCodeGenerator> Generators;

    protected BaseCodeGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
    {
        Generators = generators;
    }

    protected StatementSyntax GenerateCommand(CommandNode cmd)
    {
        if (Generators.TryGetValue(cmd.GetType(), out var generator))
            return generator.Generate(cmd);

        return SyntaxFactory.ParseStatement($"// No generator for {cmd.GetType().Name}");
    }
}