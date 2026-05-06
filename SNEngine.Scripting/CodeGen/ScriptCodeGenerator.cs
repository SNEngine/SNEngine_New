using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Final clean orchestrator for code generation.
/// </summary>
public sealed class ScriptCodeGenerator
{
    private readonly Dictionary<Type, ICommandCodeGenerator> _generators = new();

    /// <summary>
    /// Все необходимые using для сгенерированных скриптов
    /// </summary>
    private readonly string[] _defaultUsings = new[]
    {
        "SNEngine.API",
        "SNEngine.Core",
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Threading.Tasks"
    };

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

        // Формируем using директивы
        var usingDirectives = _defaultUsings
            .Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u)))
            .ToList();

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usingDirectives))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classDeclaration));

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }
}