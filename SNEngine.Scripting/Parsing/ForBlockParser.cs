using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SNEngine.Scripting.Parsing
{
    public sealed class ForBlockParser : BlockParserBase
    {
        public ForCommandNode? Parse(TokenReader reader)
        {
            if (reader.Eof || reader.Current == null) return null;

            string fullLine = reader.ConsumeFullCommandLine();
            if (!fullLine.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
                return null;

            string content = fullLine.Substring(4).Trim();
            var commaMatch = Regex.Match(content, @"^(?<var>\w+)\s*=\s*(?<init>.+?)\s*,\s*(?<cond>.+?)\s*,\s*(?<inc>.+)$");

            string variable, init, condition, increment;

            if (commaMatch.Success)
            {
                variable = commaMatch.Groups["var"].Value;
                init = $"{variable} = {commaMatch.Groups["init"].Value}";
                condition = commaMatch.Groups["cond"].Value;
                increment = commaMatch.Groups["inc"].Value;
            }
            else
            {
                var parts = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) return null;

                variable = parts[0].Contains('=') ? parts[0].Split('=')[0] : parts[0];
                init = parts[0].Contains('=') ? parts[0] : $"{parts[0]} {parts[1]} {parts[2]}";
                increment = parts[^1];
                int condStart = parts[0].Contains('=') ? 1 : 3;
                condition = string.Join(" ", parts[condStart..^1]);
            }

            // Используем защищённый CollectBody из базового класса
            var body = CollectBody(reader, "endfor");

            // Дополнительная страховка (на случай, если CollectBody не дошло до endfor)
            if (!reader.Eof && reader.Current != null &&
                reader.PeekLineContent().Trim().Equals("endfor", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine();
            }

            return new ForCommandNode(variable, init, condition, increment, body);
        }
    }
}