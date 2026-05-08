using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(ContinueCommandNode))]
public sealed class ContinueCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not ContinueCommandNode)
            return SyntaxFactory.ParseStatement("// Invalid ContinueCommandNode");

        return SyntaxFactory.ContinueStatement();
    }
}