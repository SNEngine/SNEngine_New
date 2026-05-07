using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Lexer that returns full original lines (maximum compatibility with current parsers).
    /// Already skips comments and preserves line numbers + column positions.
    /// Ready for future full tokenization (keywords, strings, operators, etc.).
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

                // Skip full-line comments (// and #)
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
        Line,           // Current mode — full line (backward compatible)
        Keyword,        // Future: if, function, print, while, etc.
        Identifier,     // Future: variable names, function names
        String,         // Future: "text"
        Number,         // Future: 42, 3.14
        Operator,       // Future: =, <, >, +, -, etc.
        Punctuation,    // Future: (, ), ,, :, etc.
        Unknown
    }

    public sealed class ScriptToken
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public int OriginalLine { get; set; }
        public int Column { get; set; }

        public override string ToString() => $"{Type}('{Value}') @ {OriginalLine}:{Column}";
    }
}