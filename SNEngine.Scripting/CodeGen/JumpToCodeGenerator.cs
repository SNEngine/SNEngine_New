using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Clean and maintainable Jump To generator.
/// Uses Block with separate statements to prevent formatting corruption.
/// </summary>
[SnCodeGenerator(typeof(JumpToCommandNode))]
public sealed class JumpToCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not JumpToCommandNode jump || string.IsNullOrWhiteSpace(jump.TargetScene))
        {
            return SyntaxFactory.ParseStatement("// ERROR: Invalid Jump To command - target scene is missing");
        }

        var target = jump.TargetScene.Trim();
        string varName = UniqueVariableNameGenerator.Generate();

        // Create separate statements — this is the most reliable way
        var statements = new[]
        {
            SyntaxFactory.ParseStatement($"// Jump To {target}"),

            SyntaxFactory.ParseStatement($"var {varName} = new {target}();"),

            SyntaxFactory.ParseStatement($"{varName}.OnLoad();"),

            SyntaxFactory.ParseStatement($"{varName}.Execute();")
        };

        // Wrap in block and normalize whitespace
        return SyntaxFactory.Block(statements)
                            .NormalizeWhitespace()
                            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
    }
}