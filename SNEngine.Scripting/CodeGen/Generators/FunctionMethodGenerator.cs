using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen.Generators;

/// <summary>
/// Generates private void FunctionName() { ... }
/// </summary>
public class FunctionMethodGenerator : BaseCodeGenerator
{
    public FunctionMethodGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators) { }

    public MemberDeclarationSyntax Generate(FunctionNode func)
    {
        var statements = func.Body.Select(GenerateCommand).ToList();

        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), func.Name)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(statements));
    }
}