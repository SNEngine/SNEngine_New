using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing;

public sealed class SwitchBlockParser : BlockParserBase
{
    public SwitchCommandNode? Parse(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null) return null;

        string fullLine = reader.ConsumeFullCommandLine();
        if (!fullLine.StartsWith("switch ", StringComparison.OrdinalIgnoreCase))
            return null;

        // Извлекаем выражение (например: playerChoice или (playerChoice))
        string expression = fullLine.Substring(7).Trim(' ', '(', ')');

        var cases = new List<SwitchCaseNode>();
        List<CommandNode>? defaultBody = null;

        while (!reader.Eof && reader.Current != null)
        {
            string line = reader.PeekLineContent().Trim();

            if (line.StartsWith("switchcase ", StringComparison.OrdinalIgnoreCase))
            {
                string caseLine = reader.ConsumeFullCommandLine();
                string caseValue = caseLine.Substring(10).Trim();

                // Собираем тело кейса до endcase / endswitch / default
                var body = CollectBody(reader, "endcase", "endswitch", "default");

                cases.Add(new SwitchCaseNode(caseValue, body));
            }
            else if (line.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine();
                defaultBody = CollectBody(reader, "endswitch");
                break;
            }
            else if (line.Equals("endswitch", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine();
                break;
            }
            else
            {
                // Пропускаем неизвестные строки (защита)
                reader.ConsumeCurrentLine();
            }
        }

        return new SwitchCommandNode(expression, cases, defaultBody);
    }
}