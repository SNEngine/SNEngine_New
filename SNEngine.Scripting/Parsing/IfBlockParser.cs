using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class IfBlockParser : BlockParserBase
    {
        public IfCommandNode? Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return null;

            string lineContent = reader.PeekLineContent().Trim();

            if (!lineContent.StartsWith("if ", StringComparison.OrdinalIgnoreCase) ||
                !lineContent.Contains("then", StringComparison.OrdinalIgnoreCase))
                return null;

            string ifLine = reader.ConsumeFullCommandLine();
            int thenPos = ifLine.IndexOf("then", StringComparison.OrdinalIgnoreCase);
            string condition = ifLine.Substring(3, thenPos - 3).Trim();

            // Используем защищённый CollectBody
            var thenBody = CollectBody(reader, "else", "endif");

            var elseBody = new List<CommandNode>();

            if (!reader.Eof && reader.Current != null &&
                reader.PeekLineContent().Trim().Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine();
                elseBody = CollectBody(reader, "endif");
            }

            return new IfCommandNode(condition, thenBody, new List<ElseIfClause>(), elseBody);
        }
    }
}