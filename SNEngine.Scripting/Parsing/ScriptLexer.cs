using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Простой и стабильный Lexer (одна строка = один токен).
    /// Используется до тех пор, пока вся система не будет полностью готова к много-токенному режиму.
    /// </summary>
    public sealed class ScriptLexer
    {
        public List<ScriptToken> Tokenize(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return new List<ScriptToken>();

            var lines = source.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var tokens = new List<ScriptToken>();

            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                string trimmed = raw.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed.StartsWith("//") || trimmed.StartsWith("#"))
                    continue;

                tokens.Add(new ScriptToken
                {
                    Type = TokenType.Line,
                    Value = trimmed,
                    OriginalLine = i + 1,
                    Column = raw.Length - raw.TrimStart().Length + 1
                });
            }

            return tokens;
        }
    }

    public enum TokenType
    {
        Line
    }

    public sealed class ScriptToken
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public int OriginalLine { get; set; }
        public int Column { get; set; }
    }
}