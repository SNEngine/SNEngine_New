using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Single point of truth for all statements.
    /// This is where we will register while, for, switch, etc. in the future.
    /// </summary>
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

            string line = reader.Current.Value;

            // if-block has highest priority
            if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("then", StringComparison.OrdinalIgnoreCase))
            {
                return _ifBlockParser.Parse(reader);
            }

            // Regular commands (print, SetVar, Jump To, Quit, etc.)
            var result = _commandParser.Parse(line);
            if (result.Success)
            {
                reader.Consume();
                return result.Value;
            }

            reader.Consume();
            return null;
        }
    }
}