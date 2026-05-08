using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Реестр специальных выражений-команд.
/// Автоматически находит ВСЕ классы, реализующие IExpressionCommandGenerator
/// (логика 1 в 1 из IfCodeGenerator)
/// </summary>
public static class SpecialExpressionRegistry
{
    private static readonly Dictionary<string, IExpressionCommandGenerator> _handlers
        = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, IExpressionCommandGenerator> Handlers => _handlers;

    static SpecialExpressionRegistry()
    {
        DiscoverAllExpressionGenerators();
    }

    private static void DiscoverAllExpressionGenerators()
    {
        var assembly = typeof(IExpressionCommandGenerator).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t =>
                typeof(IExpressionCommandGenerator).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            try
            {
                var instance = (IExpressionCommandGenerator)Activator.CreateInstance(type)!;

                // Берём префикс из имени класса (GetStringFromCodeGenerator → Get String from)
                string className = type.Name.Replace("CodeGenerator", "").Replace("Generator", "");
                string prefix = System.Text.RegularExpressions.Regex.Replace(className, "([a-z])([A-Z])", "$1 $2");

                _handlers[prefix] = instance;
                Console.WriteLine($"[SpecialExpressionRegistry] Registered: {prefix}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SpecialExpressionRegistry] Failed to create {type.Name}: {ex.Message}");
            }
        }
    }
}