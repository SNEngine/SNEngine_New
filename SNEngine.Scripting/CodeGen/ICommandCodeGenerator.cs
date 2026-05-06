using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Each command node can generate its own C# code.
/// </summary>
public interface ICommandCodeGenerator
{
    StatementSyntax Generate(CommandNode node);
}