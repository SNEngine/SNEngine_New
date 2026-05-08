using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(AssignmentCommandNode))]
public sealed class AssignmentCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not AssignmentCommandNode assign)
            return SyntaxFactory.ParseStatement("// Invalid AssignmentCommandNode");

        // Используем новый оркестратор + Scope
        return VariableExpressionOrchestrator.CreateAssignment(assign, ScopeManager.Current);
    }
}