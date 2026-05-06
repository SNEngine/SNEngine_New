using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting;

/// <summary>
/// Parser that supports main body and user-defined functions: function name() ... endfunc
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

        var lines = source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(l => l.Trim())
                          .Where(l => !string.IsNullOrWhiteSpace(l));

        string? sceneName = null;
        var commands = new List<CommandNode>();
        var functions = new List<FunctionNode>();

        FunctionNode? currentFunction = null;
        var currentBody = new List<CommandNode>();

        foreach (var line in lines)
        {
            if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
            {
                sceneName = line.Substring(5).Trim();
                continue;
            }

            if (line.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
            {
                // Finish previous function if any
                if (currentFunction != null)
                {
                    functions.Add(new FunctionNode(currentFunction.Name, currentBody));
                    currentBody = new List<CommandNode>();
                }

                var funcName = line.Substring(9)
                    .Trim()
                    .TrimEnd('(', ')')
                    .Trim();

                currentFunction = new FunctionNode(funcName, new List<CommandNode>());
                continue;
            }

            if (line.Equals("endfunc", StringComparison.OrdinalIgnoreCase))
            {
                if (currentFunction != null)
                {
                    functions.Add(new FunctionNode(currentFunction.Name, currentBody));
                    currentFunction = null;
                    currentBody = new List<CommandNode>();
                }
                continue;
            }

            // Parse command
            var result = commandParser.Parse(line);
            if (result.Success)
            {
                if (currentFunction != null)
                    currentBody.Add(result.Value);
                else
                    commands.Add(result.Value);
            }
        }

        // Don't forget the last function if file ends without "endfunc"
        if (currentFunction != null)
        {
            functions.Add(new FunctionNode(currentFunction.Name, currentBody));
        }

        return new ScriptNode(sceneName, commands, functions);
    }
}