using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class IfBlockParser
    {
        private StatementParser? _statementParser;

        public IfBlockParser() { }

        public void Initialize(StatementParser statementParser)
        {
            _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
        }

        public IfCommandNode? Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return null;

            string lineContent = reader.PeekLineContent().Trim();

            if (!lineContent.StartsWith("if ", StringComparison.OrdinalIgnoreCase) ||
                !lineContent.Contains("then", StringComparison.OrdinalIgnoreCase))
                return null;

            // Потребляем всю строку if ... then
            string ifLine = reader.ConsumeFullCommandLine();

            // Извлекаем условие
            int thenPos = ifLine.IndexOf("then", StringComparison.OrdinalIgnoreCase);
            string condition = ifLine.Substring(3, thenPos - 3).Trim();

            var thenBody = new List<CommandNode>();
            var elseBody = new List<CommandNode>();

            while (!reader.Eof && reader.Current != null)
            {
                string currentLine = reader.PeekLineContent().Trim();
                if (currentLine.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                    currentLine.Equals("endif", StringComparison.OrdinalIgnoreCase))
                    break;

                var stmt = _statementParser?.ParseNext(reader);
                if (stmt != null) thenBody.Add(stmt);
            }

            if (!reader.Eof && reader.Current != null &&
                reader.PeekLineContent().Trim().Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine(); // else

                while (!reader.Eof && reader.Current != null)
                {
                    string next = reader.PeekLineContent().Trim();
                    if (next.Equals("endif", StringComparison.OrdinalIgnoreCase))
                        break;

                    var stmt = _statementParser?.ParseNext(reader);
                    if (stmt != null) elseBody.Add(stmt);
                }
            }

            if (!reader.Eof && reader.Current != null &&
                reader.PeekLineContent().Trim().Equals("endif", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine();
            }

            return new IfCommandNode(condition, thenBody, new List<ElseIfClause>(), elseBody);
        }
    }
}