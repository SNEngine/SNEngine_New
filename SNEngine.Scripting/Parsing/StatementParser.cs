using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing;

/// <summary>
/// Главный парсер отдельных строк/команд с подробным логированием
/// </summary>
public sealed class StatementParser
{
    private readonly Parser<char, CommandNode> _commandParser;
    private readonly IfBlockParser _ifBlockParser;
    private readonly ForBlockParser _forBlockParser;

    public StatementParser(Parser<char, CommandNode> commandParser,
                           IfBlockParser ifBlockParser,
                           ForBlockParser forBlockParser)
    {
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        _ifBlockParser = ifBlockParser ?? throw new ArgumentNullException(nameof(ifBlockParser));
        _forBlockParser = forBlockParser ?? throw new ArgumentNullException(nameof(forBlockParser));
    }

    public CommandNode? ParseNext(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null)
        {
            Console.WriteLine("[StatementParser] End of file reached");
            return null;
        }

        string lineContent = reader.PeekLineContent().Trim();
        Console.WriteLine($"[StatementParser] Parsing line: \"{lineContent}\"");

        string firstWord = lineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                      .FirstOrDefault() ?? string.Empty;

        // Закрывающие блоки
        if (firstWord.Equals("endif", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("else", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endfor", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endfunc", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[StatementParser] → Closing block keyword: {firstWord}");
            return null;
        }

        // If блок
        if (lineContent.StartsWith("if ", StringComparison.OrdinalIgnoreCase) &&
            lineContent.Contains("then", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[StatementParser] → Detected IF block");
            var result1 = _ifBlockParser.Parse(reader);
            Console.WriteLine($"[StatementParser] ← IF block parsed successfully");
            return result1;
        }

        // For блок
        if (lineContent.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[StatementParser] → Detected FOR block");
            var result2 = _forBlockParser.Parse(reader);
            Console.WriteLine($"[StatementParser] ← FOR block parsed successfully");
            return result2;
        }

        // Обычная команда
        string fullCommand = reader.ConsumeFullCommandLine();
        Console.WriteLine($"[StatementParser] → Regular command: {fullCommand}");

        var result = _commandParser.Parse(fullCommand);

        if (result.Success)
        {
            Console.WriteLine($"[StatementParser] ← Parsed as: {result.Value.GetType().Name}");
            return result.Value;
        }
        else
        {
            Console.WriteLine($"[StatementParser] ⚠ Failed to parse command: {fullCommand}");
            return null;
        }
    }
}