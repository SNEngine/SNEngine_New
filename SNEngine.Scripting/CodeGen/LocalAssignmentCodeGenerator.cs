using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(LocalAssignmentCommandNode))]
public sealed class LocalAssignmentCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not LocalAssignmentCommandNode local)
            return SyntaxFactory.ParseStatement("// Invalid LocalAssignmentCommandNode");

        string varName = local.VariableName.Trim();
        string expr = local.ValueExpression.Trim();

        // Если есть вызов функции — просто генерируем var x = await func(...);
        // БЕЗ блока, чтобы переменная была видна дальше
        var match = System.Text.RegularExpressions.Regex.Match(expr, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(");

        if (match.Success)
        {
            string funcName = match.Groups[1].Value;
            if (funcName != "ToString" && funcName != "GetType")
            {
                return SyntaxFactory.ParseStatement($"var {varName} = await {expr};");
            }
        }

        // Обычная логика
        ExpressionSyntax right = VariableExpressionOrchestrator.GetExpression(expr, ScopeManager.Current);

        if (!ScopeManager.Current.IsLocal(varName))
        {
            ScopeManager.Current.Declare(varName, SymbolKind.Variable);
            var declaration = SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName("var"),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(varName),
                        null,
                        SyntaxFactory.EqualsValueClause(right))));
            return SyntaxFactory.LocalDeclarationStatement(declaration);
        }

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(varName), right));
    }
}