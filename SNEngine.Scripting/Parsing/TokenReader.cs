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

        public void Consume()
        {
            if (!Eof) _position++;
        }

        /// <summary>
        /// Собирает полную строку команды из всех токенов текущей строки.
        /// Это ключевой метод для совместимости со старым Pidgin-парсером.
        /// </summary>
        public string ConsumeFullCommandLine()
        {
            if (Eof || Current == null) return string.Empty;

            int startLine = Current.OriginalLine;
            var sb = new StringBuilder();

            while (!Eof && Current != null && Current.OriginalLine == startLine)
            {
                sb.Append(Current.Value).Append(" ");
                Consume();
            }

            return sb.ToString().Trim();
        }
    }
}