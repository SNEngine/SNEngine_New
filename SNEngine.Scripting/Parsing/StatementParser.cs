using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing
{
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
            if (reader.Eof || reader.Current == null) return null;

            string lineContent = reader.PeekLineContent().Trim();
            string firstWord = lineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                          .FirstOrDefault() ?? string.Empty;

            if (firstWord.Equals("endif", StringComparison.OrdinalIgnoreCase) ||
                firstWord.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                firstWord.Equals("endfor", StringComparison.OrdinalIgnoreCase) ||
                firstWord.Equals("endfunc", StringComparison.OrdinalIgnoreCase))
            {
                return null; // не парсим, пусть блок-парсеры сами обработают
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

            string fullCommand = reader.ConsumeFullCommandLine();
            var result = _commandParser.Parse(fullCommand);

            return result.Success ? result.Value : null;
        }
    }
}