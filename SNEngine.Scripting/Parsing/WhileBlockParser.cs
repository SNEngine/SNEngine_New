using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing;

public sealed class WhileBlockParser : BlockParserBase
{
    public WhileCommandNode? Parse(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null) return null;

        string fullLine = reader.ConsumeFullCommandLine();
        if (!fullLine.StartsWith("while ", StringComparison.OrdinalIgnoreCase))
            return null;

        string condition = fullLine.Substring(6).Trim();

        // Собираем тело до endwhile
        var body = CollectBody(reader, "endwhile");

        // Страховка
        if (!reader.Eof && reader.Current != null &&
            reader.PeekLineContent().Trim().Equals("endwhile", StringComparison.OrdinalIgnoreCase))
        {
            reader.ConsumeFullCommandLine();
        }

        return new WhileCommandNode(condition, body);
    }
}