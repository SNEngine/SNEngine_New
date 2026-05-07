using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Converts raw script text into tokens while preserving original line numbers.
    /// First step toward proper lexing (easy to extend later).
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