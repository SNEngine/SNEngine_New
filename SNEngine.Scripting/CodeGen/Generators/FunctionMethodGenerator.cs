using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen.Generators;

public class FunctionMethodGenerator : BaseCodeGenerator
{
    public FunctionMethodGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators) { }

    public MemberDeclarationSyntax Generate(FunctionNode func)
    {
        ScopeManager.Current.PushScope();

        try
        {
            var parameters = func.Parameters.Select(p =>
            {
                var param = SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                    .WithType(SyntaxFactory.ParseTypeName(p.Type));

                if (!string.IsNullOrEmpty(p.DefaultValue))
                {
                    param = param.WithDefault(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ParseExpression(p.DefaultValue)));
                }

                ScopeManager.Current.Declare(p.Name, SymbolKind.Parameter);
                return param;
            }).ToList();

            var statements = func.Body.Select(GenerateCommand).ToList();

            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), func.Name)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                .WithBody(SyntaxFactory.Block(statements));
        }
        finally
        {
            ScopeManager.Current.PopScope();
        }
    }
}