using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting;

/// <summary>
/// Working parser for .sn files (line-based but uses dynamic commands)
/// </summary>
public static class ScriptParser
{
    private static CommandParserFactory? _factory;

    public static void Initialize(CommandParserFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public static ScriptNode Parse(string source)
    {
        if (_factory == null)
            throw new InvalidOperationException("Call ScriptParser.Initialize() first.");

        var commandParser = _factory.CreateCommandParser();

        var lines = source
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        string? sceneName = null;
        var commands = new List<CommandNode>();

        foreach (var line in lines)
        {
            if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
            {
                sceneName = line.Substring(5).Trim();
                continue;
            }

            if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
                break;

            // Use dynamic parser for each line
            var result = commandParser.Parse(line);
            if (result.Success)
            {
                commands.Add(result.Value);
            }
        }

        return new ScriptNode(sceneName, commands);
    }
}