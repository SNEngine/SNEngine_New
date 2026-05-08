using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(GetStringFromCommandNode))]
public sealed class GetStringFromCodeGenerator : ICommandCodeGenerator, IExpressionCommandGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not GetStringFromCommandNode getStr)
            return SyntaxFactory.ParseStatement("// Invalid GetStringFromCommandNode");

        var expr = GenerateExpression(getStr.Expression);
        return SyntaxFactory.ExpressionStatement(expr);
    }

    public ExpressionSyntax GenerateExpression(string innerExpression)
    {
        ExpressionSyntax inner = VariableExpressionOrchestrator.GetExpression(innerExpression, ScopeManager.Current);

        var toStringAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            inner,
            SyntaxFactory.IdentifierName("ToString"));

        return SyntaxFactory.InvocationExpression(toStringAccess);
    }
}