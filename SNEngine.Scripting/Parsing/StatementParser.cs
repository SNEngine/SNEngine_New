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

        string fullCommand = reader.ConsumeFullCommandLine();
        var result2 = _commandParser.Parse(fullCommand);

        return result2.Success ? result2.Value : null;
    }
}