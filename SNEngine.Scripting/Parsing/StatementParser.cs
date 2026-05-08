using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing;

public sealed class StatementParser
{
    private readonly Parser<char, CommandNode> _commandParser;
    private readonly IfBlockParser _ifBlockParser;
    private readonly ForBlockParser _forBlockParser;
    private readonly SwitchBlockParser _switchBlockParser;
    private readonly WhileBlockParser _whileBlockParser;

    public StatementParser(Parser<char, CommandNode> commandParser,
                           IfBlockParser ifBlockParser,
                           ForBlockParser forBlockParser,
                           SwitchBlockParser switchBlockParser,
                           WhileBlockParser whileBlockParser)
    {
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        _ifBlockParser = ifBlockParser ?? throw new ArgumentNullException(nameof(ifBlockParser));
        _forBlockParser = forBlockParser ?? throw new ArgumentNullException(nameof(forBlockParser));
        _switchBlockParser = switchBlockParser ?? throw new ArgumentNullException(nameof(switchBlockParser));
        _whileBlockParser = whileBlockParser ?? throw new ArgumentNullException(nameof(whileBlockParser));
    }

    public CommandNode? ParseNext(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null)
            return null;

        string lineContent = reader.PeekLineContent().Trim();
        string firstWord = lineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                      .FirstOrDefault() ?? string.Empty;

        // Закрывающие блоки
        if (firstWord.Equals("endif", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("else", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endfor", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endcase", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endswitch", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endwhile", StringComparison.OrdinalIgnoreCase) ||
            firstWord.Equals("endfunc", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // === Специальные команды (высокий приоритет) ===
        if (firstWord.Equals("break", StringComparison.OrdinalIgnoreCase))
        {
            reader.ConsumeFullCommandLine();
            return new BreakCommandNode();
        }

        if (firstWord.Equals("continue", StringComparison.OrdinalIgnoreCase))
        {
            reader.ConsumeFullCommandLine();
            return new ContinueCommandNode();
        }

        if (firstWord.Equals("return", StringComparison.OrdinalIgnoreCase))
        {
            string fullLine = reader.ConsumeFullCommandLine();
            string value = fullLine.Length > 6 ? fullLine.Substring(6).Trim() : null;
            return new ReturnCommandNode(string.IsNullOrWhiteSpace(value) ? null : value);
        }

        if (firstWord.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            string fullLine = reader.ConsumeFullCommandLine();
            string content = fullLine.Length > 5 ? fullLine.Substring(6).Trim() : "";

            int eqIndex = content.IndexOf('=');
            string varName = eqIndex > 0 ? content.Substring(0, eqIndex).Trim() : content.Trim();
            string value = eqIndex > 0 ? content.Substring(eqIndex + 1).Trim() : "0";

            return new LocalAssignmentCommandNode(varName, value);
        }

        // === Блочные конструкции ===
        if (lineContent.StartsWith("if ", StringComparison.OrdinalIgnoreCase) &&
            lineContent.Contains("then", StringComparison.OrdinalIgnoreCase))
        {
            return _ifBlockParser.Parse(reader);
        }

        if (lineContent.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
        {
            return _forBlockParser.Parse(reader);
        }

        if (lineContent.StartsWith("switch ", StringComparison.OrdinalIgnoreCase))
        {
            return _switchBlockParser.Parse(reader);
        }

        if (lineContent.StartsWith("while ", StringComparison.OrdinalIgnoreCase))
        {
            return _whileBlockParser.Parse(reader);
        }

        // === Все остальные команды ===
        string fullCommand = reader.ConsumeFullCommandLine();
        var result2 = _commandParser.Parse(fullCommand);

        return result2.Success ? result2.Value : null;
    }
}