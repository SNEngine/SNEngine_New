using SNEngine.Scripting.Ast;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class FunctionParser
    {
        private readonly StatementParser _statementParser;

        public FunctionParser(StatementParser statementParser)
        {
            _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
        }

        public FunctionNode Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return new FunctionNode("", new List<CommandNode>());

            // Полностью потребляем строку объявления функции
            string fullLine = reader.ConsumeFullCommandLine();
            string funcName = "";

            if (fullLine.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
            {
                funcName = fullLine.Substring(9).Trim().TrimEnd('(', ')').Trim();
            }

            var body = new List<CommandNode>();

            while (!reader.Eof && reader.Current != null && !reader.Match("endfunc"))
            {
                var statement = _statementParser.ParseNext(reader);
                if (statement != null) body.Add(statement);
            }

            if (!reader.Eof && reader.Current != null && reader.Match("endfunc"))
                reader.Consume();

            return new FunctionNode(funcName, body);
        }
    }
}