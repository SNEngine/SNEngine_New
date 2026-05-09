using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(CallCommandNode))]
public sealed class CallCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not CallCommandNode call)
            return SyntaxFactory.ParseStatement("// Invalid CallCommandNode");

        string args = string.Join(", ", call.Arguments);
        return SyntaxFactory.ParseStatement($"await {call.FunctionName}({args});");
    }
}