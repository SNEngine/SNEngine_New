using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(ShowBackgroundCommandNode))]
public sealed class ShowBackgroundCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not ShowBackgroundCommandNode bg)
            return SyntaxFactory.ParseStatement("// Invalid ShowBackgroundCommandNode");

        return SyntaxFactory.ParseStatement($"BackgroundAPI.Show(\"{bg.BackgroundName}\");");
    }
}