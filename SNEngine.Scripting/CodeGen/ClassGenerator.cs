using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.API;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen.Generators;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

public class ClassGenerator : BaseCodeGenerator
{
    private readonly ExecuteMethodGenerator _executeGen;
    private readonly FunctionMethodGenerator _functionGen;

    public ClassGenerator(IReadOnlyDictionary<Type, ICommandCodeGenerator> generators)
        : base(generators)
    {
        _executeGen = new ExecuteMethodGenerator(generators);
        _functionGen = new FunctionMethodGenerator(generators);
    }

    public CompilationUnitSyntax Generate(ScriptNode script)
    {
        var sceneName = script.SceneName ?? "UnnamedScene";

        Console.WriteLine($"[ClassGenerator] Generating class: {sceneName}");

        // Группируем присваивания
        var variableGroups = script.Commands
            .OfType<AssignmentCommandNode>()
            .GroupBy(a => a.VariableName.Trim())
            .ToDictionary(g => g.Key, g => g.ToList());

        Console.WriteLine($"[ClassGenerator] Found {variableGroups.Count} unique variables");

        var members = new List<MemberDeclarationSyntax>();

        // Создаём поля
        foreach (var group in variableGroups)
        {
            var field = CreateTypedField(group.Key, group.Value);
            members.Add(field);
        }

        members.Add(ConstructorGenerator.Create(sceneName));
        members.Add(_executeGen.Generate(script.Commands));

        foreach (var func in script.Functions)
        {
            members.Add(_functionGen.Generate(func));
        }

        var classDeclaration = SyntaxFactory.ClassDeclaration(sceneName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("SNScript")))))
            .AddMembers(members.ToArray());

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName("SNEngine.Game.Scripts"))
            .AddMembers(classDeclaration);

        var unit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.Core"))
            }))
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();

        Console.WriteLine($"[ClassGenerator] Finished generating {sceneName} ({members.Count} members)\n");
        return unit;
    }

    private static FieldDeclarationSyntax CreateTypedField(string varName, List<AssignmentCommandNode> assignments)
    {
        string bestType = "var";

        Console.WriteLine($"[Type Detection] Variable '{varName}' has {assignments.Count} assignments");

        foreach (var assign in assignments)
        {
            string expr = assign.ValueExpression.Trim();
            string detected = VariableExpressionOrchestrator.GetTypeForValue(expr);

            Console.WriteLine($"    → \"{expr}\" → {detected}");

            if (IsBetterType(detected, bestType))
                bestType = detected;
        }

        Console.WriteLine($"[Type] Final: private {bestType} {varName};\n");

        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(bestType),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(varName))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    private static bool IsBetterType(string newType, string currentType)
    {
        var priority = new Dictionary<string, int>
        {
            ["double"] = 5,
            ["int"] = 4,
            ["bool"] = 3,
            ["string"] = 2,
            ["var"] = 1
        };

        return priority.GetValueOrDefault(newType, 0) > priority.GetValueOrDefault(currentType, 0);
    }
}