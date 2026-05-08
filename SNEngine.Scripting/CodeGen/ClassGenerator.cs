using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.API;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen.Generators;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Orchestrates the full class generation.
/// Automatically creates private fields with correct types using SNVariable.GetTypeForCompile().
/// </summary>
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

        // Собираем уникальные переменные из всех Assignment
        var variableAssignments = script.Commands
            .OfType<AssignmentCommandNode>()
            .GroupBy(a => a.VariableName.Trim())
            .Select(g => g.First()) // первое присваивание определяет тип
            .ToList();

        var members = new List<MemberDeclarationSyntax>();

        // 1. Генерируем private поля с правильным типом
        foreach (var assign in variableAssignments)
        {
            members.Add(CreateTypedField(assign));
        }

        // 2. Конструктор + Execute + Functions
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

        return SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.Core"))
            }))
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();
    }

    /// <summary>
    /// Создаёт private поле, используя SNVariable для вывода типа
    /// </summary>
    private static FieldDeclarationSyntax CreateTypedField(AssignmentCommandNode assign)
    {
        string varName = assign.VariableName.Trim();

        // Создаём временный SNVariable для определения типа
        object? sampleValue = TryParseValue(assign.ValueExpression);
        var tempVar = new SNVariable(sampleValue ?? 0);
        string typeName = tempVar.GetTypeForCompile();

        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(typeName),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(varName))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    private static object? TryParseValue(string expr)
    {
        expr = expr?.Trim() ?? "";

        if (int.TryParse(expr, out int i)) return i;
        if (double.TryParse(expr, out double d)) return d;
        if (bool.TryParse(expr, out bool b)) return b;
        if (expr.StartsWith("\"") && expr.EndsWith("\""))
            return expr.Trim('"');

        return expr; // fallback
    }
}