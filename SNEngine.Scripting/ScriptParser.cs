using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting;

/// <summary>
/// Parser with support for if-then-else-endif and else if chains
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
                          .Where(l => !string.IsNullOrWhiteSpace(l))
                          .ToList();

        string? sceneName = null;
        var commands = new List<CommandNode>();
        var functions = new List<FunctionNode>();

        int i = 0;
        while (i < lines.Count)
        {
            var line = lines[i];

            if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
            {
                sceneName = line.Substring(5).Trim();
                i++;
                continue;
            }

            if (line.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
            {
                var funcName = line.Substring(9).Trim().TrimEnd('(', ')').Trim();
                var funcBody = new List<CommandNode>();
                i++;

                while (i < lines.Count && !lines[i].Equals("endfunc", StringComparison.OrdinalIgnoreCase))
                {
                    var result = commandParser.Parse(lines[i]);
                    if (result.Success) funcBody.Add(result.Value);
                    i++;
                }
                functions.Add(new FunctionNode(funcName, funcBody));
                i++;
                continue;
            }

            if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase) && line.Contains("then"))
            {
                var ifNode = ParseIfBlock(lines, ref i, commandParser);
                if (ifNode != null) commands.Add(ifNode);
                continue;
            }

            // Обычная команда
            var cmdResult = commandParser.Parse(line);
            if (cmdResult.Success)
            {
                commands.Add(cmdResult.Value);
            }
            i++;
        }

        return new ScriptNode(sceneName, commands, functions);
    }

    private static IfCommandNode? ParseIfBlock(List<string> lines, ref int index, Parser<char, CommandNode> commandParser)
    {
        var line = lines[index];
        int thenPos = line.IndexOf("then", StringComparison.OrdinalIgnoreCase);
        if (thenPos < 0) return null;

        var condition = line.Substring(3, thenPos - 3).Trim();

        var thenBody = new List<CommandNode>();
        var elseIfClauses = new List<ElseIfClause>();
        var elseBody = new List<CommandNode>();

        index++;

        while (index < lines.Count)
        {
            var current = lines[index].Trim();

            if (current.Equals("endif", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                break;
            }

            if (current.StartsWith("else if ", StringComparison.OrdinalIgnoreCase))
            {
                int elseThenPos = current.IndexOf("then", StringComparison.OrdinalIgnoreCase);
                var elseIfCondition = current.Substring(7, elseThenPos - 7).Trim();

                var elseIfBody = new List<CommandNode>();
                index++;

                while (index < lines.Count &&
                       !lines[index].Equals("else", StringComparison.OrdinalIgnoreCase) &&
                       !lines[index].StartsWith("else if ", StringComparison.OrdinalIgnoreCase) &&
                       !lines[index].Equals("endif", StringComparison.OrdinalIgnoreCase))
                {
                    var parseResult = commandParser.Parse(lines[index]);   // ← переименовали
                    if (parseResult.Success) elseIfBody.Add(parseResult.Value);
                    index++;
                }

                elseIfClauses.Add(new ElseIfClause(elseIfCondition, elseIfBody));
                continue;
            }

            if (current.Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                while (index < lines.Count && !lines[index].Equals("endif", StringComparison.OrdinalIgnoreCase))
                {
                    var parseResult = commandParser.Parse(lines[index]);   // ← переименовали
                    if (parseResult.Success) elseBody.Add(parseResult.Value);
                    index++;
                }
                continue;
            }

            // Обычные команды в then
            var parseResultMain = commandParser.Parse(current);   // ← переименовали
            if (parseResultMain.Success)
                thenBody.Add(parseResultMain.Value);

            index++;
        }

        return new IfCommandNode(condition, thenBody, elseIfClauses, elseBody);
    }
}