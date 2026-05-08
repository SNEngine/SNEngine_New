using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing;

/// <summary>
/// Парсер команды local:
/// local playerHealth = 100
/// local name = "Yuki"
/// local result = a * b + 10
/// </summary>
public sealed class LocalAssignmentParser
{
    private readonly Parser<char, CommandNode> _expressionParser;

    public LocalAssignmentParser(Parser<char, CommandNode> expressionParser)
    {
        _expressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
    }

    public LocalAssignmentCommandNode Parse(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null)
            return new LocalAssignmentCommandNode("error", "0");

        string fullLine = reader.ConsumeFullCommandLine().Trim();

        Console.WriteLine($"[LocalAssignmentParser] Parsing: {fullLine}");

        // Убираем префикс "local "
        if (fullLine.StartsWith("local ", StringComparison.OrdinalIgnoreCase))
        {
            fullLine = fullLine.Substring(6).Trim();
        }

        // Разделяем на имя и значение
        int eqIndex = fullLine.IndexOf('=');
        if (eqIndex <= 0)
        {
            // Только имя без присваивания (local x)
            string varName = fullLine.Trim();
            Console.WriteLine($"[LocalAssignmentParser] → local {varName} (no value)");
            return new LocalAssignmentCommandNode(varName, "0");
        }

        string varNamePart = fullLine.Substring(0, eqIndex).Trim();
        string valuePart = fullLine.Substring(eqIndex + 1).Trim();

        Console.WriteLine($"[LocalAssignmentParser] → local {varNamePart} = {valuePart}");

        return new LocalAssignmentCommandNode(varNamePart, valuePart);
    }
}