using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;

namespace SNEngine.Scripting.CodeGen.Generators;

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
            var stmt = GenerateCommand(cmd);
            statements.Add(stmt);
        }

        var method = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.ParseTypeName("Task"), "ExecuteAsync")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(statements));

        return method;
    }
}