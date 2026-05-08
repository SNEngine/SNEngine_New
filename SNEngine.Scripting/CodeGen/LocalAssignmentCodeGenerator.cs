using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(LocalAssignmentCommandNode))]
public sealed class LocalAssignmentCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not LocalAssignmentCommandNode local)
            return SyntaxFactory.ParseStatement("// Invalid LocalAssignmentCommandNode");

        string varName = local.VariableName.Trim();
        ExpressionSyntax right = VariableExpressionOrchestrator.GetExpression(local.ValueExpression, ScopeManager.Current);

        if (!ScopeManager.Current.IsLocal(varName))
        {
            ScopeManager.Current.Declare(varName, SymbolKind.Variable);

            // Используем var — самый надёжный вариант для выражений
            var declaration = SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("var"),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(varName),
                        null,
                        SyntaxFactory.EqualsValueClause(right))));

            return SyntaxFactory.LocalDeclarationStatement(declaration);
        }

        // Если переменная уже объявлена в этом скоупе
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(varName),
                right));
    }
}