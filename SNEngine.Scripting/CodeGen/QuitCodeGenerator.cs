using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Generates call to SNEngine.API.SNEngine.Quit()
/// </summary>
[SnCodeGenerator(typeof(QuitCommandNode))]
public sealed class QuitCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not QuitCommandNode)
            return SyntaxFactory.ParseStatement("// ERROR: Invalid Quit command");

        // Генерируем: SNEngine.API.SNEngine.Quit();
        var code = "SNEngine.API.SNEngine.Quit();";

        try
        {
            return SyntaxFactory.ParseStatement(code)
                               .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                               .NormalizeWhitespace();
        }
        catch
        {
            return SyntaxFactory.ParseStatement("// Quit - failed to generate");
        }
    }
}