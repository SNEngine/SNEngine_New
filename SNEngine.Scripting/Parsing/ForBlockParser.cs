using Pidgin;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class ForBlockParser
    {
        private StatementParser? _statementParser;

        public ForBlockParser() { }

        public void Initialize(StatementParser statementParser)
        {
            _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
        }

        public ForCommandNode? Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return null;

            string fullLine = reader.ConsumeFullCommandLine();

            if (!fullLine.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
                return null;

            string content = fullLine.Substring(4).Trim();

            // Разбор: for i = 0 i < 5 i++
            var parts = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return null;

            string variable = parts[0];
            string init = string.Join(" ", parts[0..3]);           // i = 0
            string condition = parts[2] + " " + parts[3];          // i < 5
            string increment = parts.Length > 4 ? string.Join(" ", parts[4..]) : parts[3];

            var body = new List<CommandNode>();

            while (!reader.Eof && reader.Current != null)
            {
                string current = reader.Current.Value.Trim();
                if (current.Equals("endfor", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Consume();
                    break;
                }

                var stmt = _statementParser?.ParseNext(reader);
                if (stmt != null) body.Add(stmt);
            }

            return new ForCommandNode(variable, init, condition, increment, body);
        }
    }
}