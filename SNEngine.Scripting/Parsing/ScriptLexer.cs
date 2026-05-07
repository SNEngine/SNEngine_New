using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Lexer with automatic keyword registration from [SnCommand] attributes.
    /// Only core language syntax is hardcoded.
    /// </summary>
    public sealed class ScriptLexer
    {
        // === Только базовый синтаксис языка (не команды) ===
        private static readonly HashSet<string> _registeredKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "if", "then", "else", "endif", "else if",
            "function", "endfunc",
            "name"
            // print, SetVar, Quit, Jump и остальные — регистрируются автоматически
        };

        /// <summary>
        /// Автоматически регистрирует все команды из [SnCommand] атрибутов.
        /// </summary>
        public static void AutoRegisterAllCommands()
        {
            var assembly = Assembly.GetExecutingAssembly(); // или Assembly.GetEntryAssembly()

            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<SnCommandAttribute>() != null);

            foreach (var type in commandTypes)
            {
                var attr = type.GetCustomAttribute<SnCommandAttribute>();
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Keyword))
                {
                    _registeredKeywords.Add(attr.Keyword.Trim());
                }
            }
        }

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

                TokenType type = TokenType.Line;

                int firstSpace = trimmed.IndexOf(' ');
                string firstWord = (firstSpace > 0) ? trimmed.Substring(0, firstSpace) : trimmed;

                if (_registeredKeywords.Contains(firstWord))
                {
                    type = TokenType.Keyword;
                }

                tokens.Add(new ScriptToken
                {
                    Type = type,
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
        Line,
        Keyword,
        Identifier,
        String,
        Number,
        Operator,
        Punctuation,
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