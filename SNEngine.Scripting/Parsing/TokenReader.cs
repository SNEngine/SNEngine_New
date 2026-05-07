using System;
using System.Collections.Generic;
using System.Text;

namespace SNEngine.Scripting.Parsing
{
    public sealed class TokenReader
    {
        private readonly IReadOnlyList<ScriptToken> _tokens;
        private int _position = 0;

        public TokenReader(IReadOnlyList<ScriptToken> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public bool Eof => _position >= _tokens.Count;
        public ScriptToken? Current => Eof ? null : _tokens[_position];

        public bool Match(string prefix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return Current != null && Current.Value.StartsWith(prefix, comparison);
        }

        public void Consume() => _position++;

        public string ConsumeFullCommandLine()
        {
            if (Eof || Current == null) return string.Empty;

            int startLine = Current.OriginalLine;
            var sb = new StringBuilder();

            while (!Eof && Current != null && Current.OriginalLine == startLine)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(Current.Value);
                Consume();
            }

            return sb.ToString().Trim();
        }

        public string PeekLineContent()
        {
            if (Eof || Current == null) return string.Empty;

            int lineNum = Current.OriginalLine;
            var sb = new StringBuilder();

            for (int i = _position; i < _tokens.Count; i++)
            {
                if (_tokens[i].OriginalLine != lineNum) break;
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(_tokens[i].Value);
            }

            return sb.ToString();
        }
    }
}