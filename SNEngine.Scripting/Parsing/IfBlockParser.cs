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
            if (reader.Eof) return null;

            string line = reader.Current.Value;
            int thenPos = line.IndexOf("then", StringComparison.OrdinalIgnoreCase);

            if (thenPos < 0)
            {
                reader.Consume();
                return null;
            }

            string condition = line.Substring(3, thenPos - 3).Trim();
            var thenBody = new List<CommandNode>();
            var elseIfClauses = new List<ElseIfClause>();
            var elseBody = new List<CommandNode>();

            reader.Consume();

            int safety = 0;
            while (!reader.Eof && safety++ < 10000)
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
                    if (elseThenPos > 7)
                    {
                        string elseIfCondition = current.Substring(7, elseThenPos - 7).Trim();
                        var elseIfBody = new List<CommandNode>();
                        reader.Consume();

                        while (!reader.Eof &&
                               !reader.Match("else") &&
                               !reader.Match("else if ") &&
                               !reader.Match("endif"))
                        {
                            var stmt = _statementParser?.ParseNext(reader);
                            if (stmt != null) elseIfBody.Add(stmt);
                        }
                        elseIfClauses.Add(new ElseIfClause(elseIfCondition, elseIfBody));
                    }
                    else
                    {
                        reader.Consume();
                    }
                    continue;
                }

                if (current.Equals("else", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Consume();
                    while (!reader.Eof && !reader.Match("endif"))
                    {
                        var stmt = _statementParser?.ParseNext(reader);
                        if (stmt != null) elseBody.Add(stmt);
                    }
                    continue;
                }

                var stmtMain = _statementParser?.ParseNext(reader);
                if (stmtMain != null)
                {
                    thenBody.Add(stmtMain);
                }
            }

            return new IfCommandNode(condition, thenBody, elseIfClauses, elseBody);
        }
    }
}