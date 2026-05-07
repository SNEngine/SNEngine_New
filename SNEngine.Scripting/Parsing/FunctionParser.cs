using SNEngine.Scripting.Ast;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Responsible ONLY for function ... endfunc syntax.
    /// Does NOT know anything about if/while/for — delegates to StatementParser.
    /// This keeps it small and stable even when we add many new constructs.
    /// </summary>
    public sealed class FunctionParser
    {
        private readonly StatementParser _statementParser;

        public FunctionParser(StatementParser statementParser)
        {
            _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
        }

        public FunctionNode Parse(TokenReader reader)
        {
            string line = reader.Current.Value;
            string funcName = line.Substring(9).Trim().TrimEnd('(', ')').Trim();
            var body = new List<CommandNode>();

            reader.Consume(); // consume function declaration line

            while (!reader.Eof && !reader.Match("endfunc"))
            {
                var statement = _statementParser.ParseNext(reader);
                if (statement != null)
                    body.Add(statement);
            }

            if (!reader.Eof)
                reader.Consume(); // consume "endfunc"

            return new FunctionNode(funcName, body);
        }
    }
}