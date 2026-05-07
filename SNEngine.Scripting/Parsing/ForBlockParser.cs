using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            var commaMatch = Regex.Match(content, @"^(?<var>\w+)\s*=\s*(?<init>.+?)\s*,\s*(?<cond>.+?)\s*,\s*(?<inc>.+)$");

            if (commaMatch.Success)
            {
                string varName = commaMatch.Groups["var"].Value;
                return CreateForNode(varName, $"{varName} = {commaMatch.Groups["init"].Value}", commaMatch.Groups["cond"].Value, commaMatch.Groups["inc"].Value, reader);
            }

            var parts = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                string variable = parts[0].Contains('=') ? parts[0].Split('=')[0] : parts[0];
                string init = parts[0].Contains('=') ? parts[0] : $"{parts[0]} {parts[1]} {parts[2]}";
                string increment = parts[^1];
                int conditionStart = parts[0].Contains('=') ? 1 : 3;
                string condition = string.Join(" ", parts[conditionStart..^1]);

                return CreateForNode(variable, init, condition, increment, reader);
            }

            return null;
        }

        private ForCommandNode CreateForNode(string variable, string init, string condition, string increment, TokenReader reader)
        {
            var body = new List<CommandNode>();

            while (!reader.Eof && reader.Current != null)
            {
                string currentLine = reader.PeekLineContent().Trim();

                if (currentLine.Equals("endfor", StringComparison.OrdinalIgnoreCase))
                {
                    reader.ConsumeCurrentLine();
                    break;
                }

                var stmt = _statementParser?.ParseNext(reader);
                if (stmt != null)
                {
                    body.Add(stmt);
                }
                else
                {
                    if (!reader.Eof && !currentLine.Equals("endfor", StringComparison.OrdinalIgnoreCase))
                    {
                        reader.ConsumeCurrentLine();
                    }
                }
            }

            return new ForCommandNode(variable, init, condition, increment, body);
        }
    }
}