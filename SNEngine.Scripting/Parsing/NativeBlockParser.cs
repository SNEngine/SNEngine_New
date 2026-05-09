using SNEngine.Scripting.Ast;
using SNEngine.Scripting.AST;
using System;
using System.Text;

namespace SNEngine.Scripting.Parsing;

/// <summary>
/// Parser for native C# code blocks: native ... endnative
/// </summary>
public sealed class NativeBlockParser : BlockParserBase
{
    public CommandNode? Parse(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null)
            return null;

        string line = reader.PeekLineContent().Trim();
        if (!line.StartsWith("native", StringComparison.OrdinalIgnoreCase))
            return null;

        int startLine = GetCurrentLine(reader);

        reader.ConsumeFullCommandLine(); // consume "native" line

        var codeBuilder = new StringBuilder();

        while (!reader.Eof && reader.Current != null)
        {
            string currentLine = reader.PeekLineContent().Trim();

            if (currentLine.Equals("endnative", StringComparison.OrdinalIgnoreCase))
            {
                reader.ConsumeFullCommandLine(); // consume "endnative"
                break;
            }

            // Preserve exact formatting and indentation
            codeBuilder.AppendLine(reader.ConsumeFullCommandLine());
        }

        string rawCode = codeBuilder.ToString().Trim();

        return new NativeCommandNode(rawCode, startLine);
    }

    private static int GetCurrentLine(TokenReader reader)
    {
        return reader.Current?.OriginalLine ?? 0;
    }
}