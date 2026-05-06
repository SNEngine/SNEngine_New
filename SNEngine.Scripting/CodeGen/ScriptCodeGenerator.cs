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
/// Now works with ClassGenerator that returns full CompilationUnit.
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

        // Теперь ClassGenerator возвращает полный CompilationUnitSyntax
        var compilationUnit = classGenerator.Generate(script);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }
}