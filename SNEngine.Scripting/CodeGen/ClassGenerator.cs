using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen.Generators;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Orchestrates the full class generation
/// </summary>
public class ClassGenerator : BaseCodeGenerator
{
    private readonly ExecuteMethodGenerator _executeGen;
    private readonly FunctionMethodGenerator _functionGen;

    public ClassGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators) : base(generators)
    {
        _executeGen = new ExecuteMethodGenerator(generators);
        _functionGen = new FunctionMethodGenerator(generators);
    }

    public MemberDeclarationSyntax Generate(ScriptNode script)
    {
        var sceneName = script.SceneName ?? "UnnamedScene";

        var members = new List<MemberDeclarationSyntax>
        {
            ConstructorGenerator.Create(sceneName),
            _executeGen.Generate(script.Commands)
        };

        foreach (var func in script.Functions)
        {
            members.Add(_functionGen.Generate(func));
        }

        return SyntaxFactory.ClassDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("SNScript")))))
            .AddMembers(members.ToArray());
    }
}