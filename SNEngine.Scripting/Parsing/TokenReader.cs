using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Safe cursor for navigating tokens with lookahead and consumption.
    /// </summary>
    public sealed class TokenReader
    {
        private readonly IReadOnlyList<ScriptToken> _tokens;
        private int _position = 0;

        public TokenReader(IReadOnlyList<ScriptToken> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public bool Eof => _position >= _tokens.Count;
        public ScriptToken Current => Eof ? throw new InvalidOperationException("Reader is at end") : _tokens[_position];

        public bool Match(string prefix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (Eof) return false;
            return Current.Value.StartsWith(prefix, comparison);
        }

        public void Consume() => _position++;

        public string ConsumeValueAfter(string prefix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (!Match(prefix, comparison)) return string.Empty;
            string value = Current.Value.Substring(prefix.Length).Trim();
            Consume();
            return value;
        }
    }
}