using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Responsible for parsing function ... endfunc blocks.
    /// </summary>
    public sealed class FunctionParser
    {
        private readonly Parser<char, CommandNode> _commandParser;

        public FunctionParser(Parser<char, CommandNode> commandParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        }

        public FunctionNode Parse(TokenReader reader)
        {
            string line = reader.Current.Value;
            string funcName = line.Substring(9).Trim().TrimEnd('(', ')').Trim();
            var body = new List<CommandNode>();

            reader.Consume(); // consume function declaration

            while (!reader.Eof && !reader.Match("endfunc"))
            {
                var result = _commandParser.Parse(reader.Current.Value);
                if (result.Success)
                    body.Add(result.Value);

                reader.Consume();
            }

            reader.Consume(); // consume "endfunc"
            return new FunctionNode(funcName, body);
        }
    }
}