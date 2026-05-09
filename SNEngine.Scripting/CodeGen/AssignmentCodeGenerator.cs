using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(AssignmentCommandNode))]
public sealed class AssignmentCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not AssignmentCommandNode assign)
            return SyntaxFactory.ParseStatement("// Invalid AssignmentCommandNode");

        string varName = assign.VariableName.Trim();
        string expr = assign.ValueExpression.Trim();

        var (stmts, finalExpr) = ExpressionHelper.WrapWithTempIfNeeded(expr);

        if (stmts.Count > 0)
        {
            stmts.Add(SyntaxFactory.ParseStatement($"{varName} = {finalExpr};"));
            return SyntaxFactory.Block(stmts);
        }

        ExpressionSyntax right = VariableExpressionOrchestrator.GetExpression(expr, ScopeManager.Current);
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(varName), right));
    }
}