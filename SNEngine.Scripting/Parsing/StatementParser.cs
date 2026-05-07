using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing
{
    public sealed class StatementParser
    {
        private readonly Parser<char, CommandNode> _commandParser;
        private readonly IfBlockParser _ifBlockParser;

        public StatementParser(Parser<char, CommandNode> commandParser, IfBlockParser ifBlockParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
            _ifBlockParser = ifBlockParser ?? throw new ArgumentNullException(nameof(ifBlockParser));
        }

        public CommandNode? ParseNext(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return null;

            string lineContent = reader.PeekLineContent();

            if (lineContent.StartsWith("if ", StringComparison.OrdinalIgnoreCase) &&
                lineContent.Contains("then", StringComparison.OrdinalIgnoreCase))
            {
                return _ifBlockParser.Parse(reader);
            }

            string fullCommand = reader.ConsumeFullCommandLine();
            var result = _commandParser.Parse(fullCommand);
            if (result.Success) return result.Value;

            return null;
        }
    }
}