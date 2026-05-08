using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(BreakCommandNode))]
public sealed class BreakCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not BreakCommandNode)
            return SyntaxFactory.ParseStatement("// Invalid BreakCommandNode");

        return SyntaxFactory.BreakStatement();
    }
}