using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.CodeGen.Generators;
using System.Collections.Generic;
using System.Reflection;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Final clean orchestrator for code generation.
/// No logic inside — only delegation to specialized generators.
/// </summary>
public sealed class ScriptCodeGenerator
{
    private readonly Dictionary<Type, ICommandCodeGenerator> _generators = new();

    /// <summary>
    /// Register all ICommandCodeGenerator implementations via attributes
    /// </summary>
    public void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<SnCodeGeneratorAttribute>();
            if (attr == null) continue;

            if (Activator.CreateInstance(type) is ICommandCodeGenerator instance)
            {
                _generators[attr.TargetNodeType] = instance;
            }
        }
    }

    public string Generate(ScriptNode script)
    {
        var classGenerator = new ClassGenerator(_generators);
        var classDeclaration = classGenerator.Generate(script);

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("SNEngine.API")),
            }))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classDeclaration));

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }
}