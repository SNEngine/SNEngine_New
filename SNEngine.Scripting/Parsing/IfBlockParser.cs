using Pidgin;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class IfBlockParser
    {
        private readonly Parser<char, CommandNode> _commandParser;
        private readonly StatementParser _statementParser;

        public IfBlockParser(Parser<char, CommandNode> commandParser, StatementParser statementParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
            _statementParser = statementParser;
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

            int safety = 0;
            while (!reader.Eof && safety++ < 10000)   // ← ЗАЩИТА ОТ ЗАВИСАНИЯ
            {
                string current = reader.Current.Value.Trim();

                if (current.Equals("endif", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Consume();
                    break;
                }

                if (current.StartsWith("else if ", StringComparison.OrdinalIgnoreCase))
                {
                    // ... (код как раньше)
                    int elseThenPos = current.IndexOf("then", StringComparison.OrdinalIgnoreCase);
                    string elseIfCondition = current.Substring(7, elseThenPos - 7).Trim();
                    var elseIfBody = new List<CommandNode>();
                    reader.Consume();

                    while (!reader.Eof && !reader.Match("else") && !reader.Match("else if ") && !reader.Match("endif"))
                    {
                        var stmt = _statementParser?.ParseNext(reader) ?? _commandParser.Parse(reader.Current.Value).Value;
                        if (stmt != null) elseIfBody.Add(stmt);
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
                        var stmt = _statementParser?.ParseNext(reader) ?? _commandParser.Parse(reader.Current.Value).Value;
                        if (stmt != null) elseBody.Add(stmt);
                        reader.Consume();
                    }
                    continue;
                }

                var stmtMain = _statementParser?.ParseNext(reader) ?? _commandParser.Parse(current).Value;
                if (stmtMain != null) thenBody.Add(stmtMain);
                reader.Consume();
            }

            if (safety >= 10000)
                Console.WriteLine("[Warning] Possible infinite loop in if-block — safety limit reached");

            return new IfCommandNode(condition, thenBody, elseIfClauses, elseBody);
        }
    }
}