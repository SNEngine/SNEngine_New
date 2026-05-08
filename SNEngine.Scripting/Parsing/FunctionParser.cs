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
            return new FunctionNode("", Array.Empty<FunctionParameter>(), "void", Array.Empty<CommandNode>());

        string fullLine = reader.ConsumeFullCommandLine().Trim();

        string name = "";
        var parameters = new List<FunctionParameter>();
        string returnType = "void";

        if (fullLine.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
        {
            string signature = fullLine.Substring(9).Trim();

            // Ищем "returned"
            int returnedIndex = signature.IndexOf("returned", StringComparison.OrdinalIgnoreCase);
            if (returnedIndex > 0)
            {
                returnType = signature.Substring(returnedIndex + 8).Trim();
                signature = signature.Substring(0, returnedIndex).Trim();
            }

            int openParen = signature.IndexOf('(');
            if (openParen > 0)
            {
                name = signature.Substring(0, openParen).Trim();
                string paramPart = signature.Substring(openParen + 1).TrimEnd(')').Trim();

                if (!string.IsNullOrEmpty(paramPart))
                {
                    foreach (var p in paramPart.Split(',').Select(x => x.Trim()))
                    {
                        var eq = p.IndexOf('=');
                        string decl = eq > 0 ? p.Substring(0, eq).Trim() : p;
                        string? def = eq > 0 ? p.Substring(eq + 1).Trim() : null;

                        var parts = decl.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                            parameters.Add(new FunctionParameter(parts[0], parts[1], def));
                        else if (parts.Length == 1)
                            parameters.Add(new FunctionParameter("var", parts[0], def));
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

        return new FunctionNode(name, parameters, returnType, body);
    }
}