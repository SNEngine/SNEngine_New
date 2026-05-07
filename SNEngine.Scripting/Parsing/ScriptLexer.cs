using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Мульти-токенный Lexer. Одна строка = несколько токенов (слова + строковые литералы).
    /// Полностью поддерживает кавычки с пробелами внутри строк.
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

                var lineTokens = TokenizeLine(raw, i + 1);
                tokens.AddRange(lineTokens);
            }

            return tokens;
        }

        private List<ScriptToken> TokenizeLine(string line, int originalLine)
        {
            var tokens = new List<ScriptToken>();
            int pos = 0;

            while (pos < line.Length)
            {
                // Пропускаем whitespace
                while (pos < line.Length && char.IsWhiteSpace(line[pos]))
                    pos++;

                if (pos >= line.Length) break;

                int tokenStart = pos;
                int column = tokenStart + 1;

                if (line[pos] == '"')
                {
                    // Полный строковый литерал (включая кавычки) — чтобы реконструкция была точной
                    pos++; // opening "
                    while (pos < line.Length && line[pos] != '"')
                        pos++;
                    if (pos < line.Length && line[pos] == '"')
                        pos++; // closing "

                    string value = line.Substring(tokenStart, pos - tokenStart);

                    tokens.Add(new ScriptToken
                    {
                        Type = TokenType.StringLiteral,
                        Value = value,
                        OriginalLine = originalLine,
                        Column = column
                    });
                }
                else
                {
                    // Обычное слово (включая "name:", "MyFunc()", etc.)
                    while (pos < line.Length && !char.IsWhiteSpace(line[pos]))
                        pos++;

                    string value = line.Substring(tokenStart, pos - tokenStart);

                    tokens.Add(new ScriptToken
                    {
                        Type = TokenType.Word,
                        Value = value,
                        OriginalLine = originalLine,
                        Column = column
                    });
                }
            }

            return tokens;
        }
    }

    public enum TokenType
    {
        Word,
        StringLiteral
    }

    public sealed class ScriptToken
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public int OriginalLine { get; set; }
        public int Column { get; set; }
    }
}