using Pidgin;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class IfBlockParser
    {
        private readonly Parser<char, CommandNode> _commandParser;
        private StatementParser? _statementParser;

        public IfBlockParser(Parser<char, CommandNode> commandParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        }

        public void Initialize(StatementParser statementParser)
        {
            _statementParser = statementParser;
        }

        public IfCommandNode? Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return null;

            string line = reader.Current.Value;

            if (!line.StartsWith("if ", StringComparison.OrdinalIgnoreCase) ||
                !line.Contains("then", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            int thenPos = line.IndexOf("then", StringComparison.OrdinalIgnoreCase);
            string condition = line.Substring(3, thenPos - 3).Trim();

            reader.Consume();

            var thenBody = new List<CommandNode>();
            var elseBody = new List<CommandNode>();

            while (!reader.Eof && reader.Current != null)
            {
                string current = reader.Current.Value.Trim();
                if (current.Equals("else", StringComparison.OrdinalIgnoreCase) || current.Equals("endif", StringComparison.OrdinalIgnoreCase))
                    break;

                var stmt = _statementParser?.ParseNext(reader);
                if (stmt != null) thenBody.Add(stmt);
            }

            if (!reader.Eof && reader.Current != null && reader.Current.Value.Trim().Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                reader.Consume();
                while (!reader.Eof && reader.Current != null && !reader.Match("endif"))
                {
                    var stmt = _statementParser?.ParseNext(reader);
                    if (stmt != null) elseBody.Add(stmt);
                }
            }

            if (!reader.Eof && reader.Current != null && reader.Match("endif"))
                reader.Consume();

            return new IfCommandNode(condition, thenBody, new List<ElseIfClause>(), elseBody);
        }
    }
}