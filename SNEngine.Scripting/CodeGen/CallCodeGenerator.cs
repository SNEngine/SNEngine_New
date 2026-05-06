using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Generates: hello();
/// </summary>
[SnCodeGenerator(typeof(CallCommandNode))]
public sealed class CallCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not CallCommandNode call)
            return SyntaxFactory.ParseStatement("// Invalid CallCommandNode");

        return SyntaxFactory.ParseStatement($"{call.FunctionName}();");
    }
}