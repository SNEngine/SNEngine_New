using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class FunctionParser : BlockParserBase
    {
        public FunctionNode Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null)
                return new FunctionNode("", new List<CommandNode>());

            string fullLine = reader.ConsumeFullCommandLine();
            string funcName = fullLine.StartsWith("function ", StringComparison.OrdinalIgnoreCase)
                ? fullLine.Substring(9).Trim().TrimEnd('(', ')').Trim()
                : "";

            var body = CollectBody(reader, "endfunc");

            if (!reader.Eof && reader.Current != null &&
                reader.PeekLineContent().Trim().StartsWith("endfunc", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine();
            }

            return new FunctionNode(funcName, body);
        }
    }
}