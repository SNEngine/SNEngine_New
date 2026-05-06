using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Generates the main Execute() method
/// </summary>
public class ExecuteMethodGenerator : BaseCodeGenerator
{
    public ExecuteMethodGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators) { }

    public MemberDeclarationSyntax Generate(IReadOnlyList<CommandNode> commands)
    {
        var statements = new List<StatementSyntax>
    {
        SyntaxFactory.ParseStatement("SNEngine.API.SNEngine.LoadEmptyScene();")
    };

        foreach (var cmd in commands)
        {
            statements.Add(GenerateCommand(cmd));   // ← теперь поддерживает if
        }

        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Execute")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(statements));
    }
}