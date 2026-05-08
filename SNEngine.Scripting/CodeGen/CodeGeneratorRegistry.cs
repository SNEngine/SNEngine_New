using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Центральный реестр всех ICommandCodeGenerator.
/// Находит их один раз при старте через рефлексию (логика из IfCodeGenerator).
/// </summary>
public static class CodeGeneratorRegistry
{
    private static readonly Dictionary<Type, ICommandCodeGenerator> _generators
        = new();

    public static IReadOnlyDictionary<Type, ICommandCodeGenerator> Generators => _generators;

    static CodeGeneratorRegistry()
    {
        DiscoverAllGenerators();
    }

    private static void DiscoverAllGenerators()
    {
        var assembly = typeof(ICommandCodeGenerator).Assembly;

        var generatorTypes = assembly.GetTypes()
            .Where(t =>
                typeof(ICommandCodeGenerator).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract);

        foreach (var type in generatorTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(type) as ICommandCodeGenerator;
                if (instance == null) continue;

                // Берём TargetNodeType из атрибута [SnCodeGenerator]
                var attr = type.GetCustomAttribute<SnCodeGeneratorAttribute>();
                Type? targetType = attr?.TargetNodeType ?? type;

                _generators[targetType] = instance;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CodeGeneratorRegistry] Failed to create {type.Name}: {ex.Message}");
            }
        }
    }

    public static ICommandCodeGenerator? GetGenerator(Type commandType)
    {
        return _generators.TryGetValue(commandType, out var gen) ? gen : null;
    }
}