using Pidgin;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Responsible for parsing if ... then ... else if ... else ... endif blocks.
    /// </summary>
    public sealed class IfBlockParser
    {
        private readonly Parser<char, CommandNode> _commandParser;

        public IfBlockParser(Parser<char, CommandNode> commandParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        }

        public IfCommandNode? Parse(TokenReader reader)
        {
            string line = reader.Current.Value;
            int thenPos = line.IndexOf("then", StringComparison.OrdinalIgnoreCase);
            if (thenPos < 0) return null;

            string condition = line.Substring(3, thenPos - 3).Trim();
            var thenBody = new List<CommandNode>();
            var elseIfClauses = new List<ElseIfClause>();
            var elseBody = new List<CommandNode>();

            reader.Consume();

            while (!reader.Eof)
            {
                string current = reader.Current.Value.Trim();

                if (current.Equals("endif", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Consume();
                    break;
                }

                if (current.StartsWith("else if ", StringComparison.OrdinalIgnoreCase))
                {
                    int elseThenPos = current.IndexOf("then", StringComparison.OrdinalIgnoreCase);
                    string elseIfCondition = current.Substring(7, elseThenPos - 7).Trim();

                    var elseIfBody = new List<CommandNode>();
                    reader.Consume();

                    while (!reader.Eof &&
                           !reader.Match("else") &&
                           !reader.Match("else if ") &&
                           !reader.Match("endif"))
                    {
                        var result = _commandParser.Parse(reader.Current.Value);
                        if (result.Success) elseIfBody.Add(result.Value);
                        reader.Consume();
                    }

                    elseIfClauses.Add(new ElseIfClause(elseIfCondition, elseIfBody));
                    continue;
                }

                if (current.Equals("else", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Consume();
                    while (!reader.Eof && !reader.Match("endif"))
                    {
                        var result = _commandParser.Parse(reader.Current.Value);
                        if (result.Success) elseBody.Add(result.Value);
                        reader.Consume();
                    }
                    continue;
                }

                // Command inside then body
                var cmdResult = _commandParser.Parse(current);
                if (cmdResult.Success)
                    thenBody.Add(cmdResult.Value);

                reader.Consume();
            }

            return new IfCommandNode(condition, thenBody, elseIfClauses, elseBody);
        }
    }
}