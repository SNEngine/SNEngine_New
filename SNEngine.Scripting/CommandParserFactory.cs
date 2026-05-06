using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Pidgin.Parser;

namespace SNEngine.Scripting;

/// <summary>
/// Автоматически находит и регистрирует все команды
/// </summary>
public sealed class CommandParserFactory
{
    private readonly Dictionary<string, Parser<char, CommandNode>> _parsers = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<SnCommandAttribute>();
            if (attr == null) continue;

            if (typeof(IParsableCommand).IsAssignableFrom(type))
            {
                var parserProp = type.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                if (parserProp?.GetValue(null) is Parser<char, CommandNode> parser)
                {
                    _parsers[attr.Keyword] = parser;
                }
            }
        }
    }

    public Parser<char, CommandNode> CreateCommandParser()
    {
        if (_parsers.Count == 0)
            throw new InvalidOperationException("No commands registered.");

        // Более длинные ключевые слова первыми (Show Background > Show Character)
        var sorted = _parsers
            .OrderByDescending(kv => kv.Key.Length)
            .Select(kv => Try(kv.Value))
            .ToArray();

        return OneOf(sorted);
    }
}