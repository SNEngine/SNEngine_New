using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(ReturnCommandNode))]
public sealed class ReturnCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not ReturnCommandNode ret)
            return SyntaxFactory.ParseStatement("// Invalid ReturnCommandNode");

        if (string.IsNullOrWhiteSpace(ret.ReturnValue))
            return SyntaxFactory.ReturnStatement();           // ← return;

        var expr = SyntaxFactory.ParseExpression(ret.ReturnValue);
        return SyntaxFactory.ReturnStatement(expr);           // ← return <expression>;
    }
}