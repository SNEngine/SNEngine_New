using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Scripting.Parsing;

public sealed class FunctionParser : BlockParserBase
{
    public FunctionNode Parse(TokenReader reader)
    {
        if (reader.Eof || reader.Current == null)
            return new FunctionNode("", Array.Empty<FunctionParameter>(), Array.Empty<CommandNode>());

        string fullLine = reader.ConsumeFullCommandLine().Trim();

        string name = "";
        var parameters = new List<FunctionParameter>();

        if (fullLine.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
        {
            string signature = fullLine.Substring(9).Trim();

            int openParen = signature.IndexOf('(');
            if (openParen > 0)
            {
                name = signature.Substring(0, openParen).Trim();
                string paramPart = signature.Substring(openParen + 1).TrimEnd(')').Trim();

                if (!string.IsNullOrEmpty(paramPart))
                {
                    var paramList = paramPart.Split(',').Select(p => p.Trim());
                    foreach (var p in paramList)
                    {
                        // Поддержка: int a = 10
                        var eqIndex = p.IndexOf('=');
                        string paramDecl = eqIndex > 0 ? p.Substring(0, eqIndex).Trim() : p;
                        string? defaultValue = eqIndex > 0 ? p.Substring(eqIndex + 1).Trim() : null;

                        var parts = paramDecl.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            parameters.Add(new FunctionParameter(parts[0], parts[1], defaultValue));
                        }
                        else if (parts.Length == 1)
                        {
                            parameters.Add(new FunctionParameter("var", parts[0], defaultValue));
                        }
                    }
                }
            }
            else
            {
                name = signature.Trim();
            }
        }

        var body = CollectBody(reader, "endfunc");

        if (!reader.Eof && reader.Current != null &&
            reader.PeekLineContent().Trim().StartsWith("endfunc", StringComparison.OrdinalIgnoreCase))
        {
            reader.ConsumeFullCommandLine();
        }

        return new FunctionNode(name, parameters, body);
    }
}