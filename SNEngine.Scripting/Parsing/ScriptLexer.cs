using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing;

/// <summary>
/// Мульти-токенный Lexer с подробным логированием.
/// Одна строка = несколько токенов (слова + строковые литералы).
/// </summary>
public sealed class ScriptLexer
{
    public List<ScriptToken> Tokenize(string source)
    {
        Console.WriteLine("[ScriptLexer] === START TOKENIZATION ===");
        Console.WriteLine($"[ScriptLexer] Source length: {source?.Length ?? 0} characters");

        if (string.IsNullOrWhiteSpace(source))
        {
            Console.WriteLine("[ScriptLexer] Empty source → returning empty token list");
            return new List<ScriptToken>();
        }

        var lines = source.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var tokens = new List<ScriptToken>();

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            string trimmed = raw.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                // Console.WriteLine($"[ScriptLexer] Line {i+1}: empty or whitespace");
                continue;
            }

            if (trimmed.StartsWith("//") || trimmed.StartsWith("#"))
            {
                Console.WriteLine($"[ScriptLexer] Line {i + 1}: comment skipped → {trimmed}");
                continue;
            }

            Console.WriteLine($"[ScriptLexer] Line {i + 1}: \"{raw}\"");
            var lineTokens = TokenizeLine(raw, i + 1);
            tokens.AddRange(lineTokens);
        }

        Console.WriteLine($"[ScriptLexer] === TOKENIZATION FINISHED === Total tokens: {tokens.Count}\n");
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
                // Строковый литерал
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

                Console.WriteLine($"[ScriptLexer]   → StringLiteral: {value}");
            }
            else
            {
                // Обычное слово
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

                Console.WriteLine($"[ScriptLexer]   → Word: {value}");
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