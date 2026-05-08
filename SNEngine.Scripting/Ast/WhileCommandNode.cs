using System.Collections.Generic;

namespace SNEngine.Scripting.Ast;

public sealed class WhileCommandNode : CommandNode
{
    public string Condition { get; }
    public List<CommandNode> Body { get; }

    public WhileCommandNode(string condition, List<CommandNode> body)
    {
        Condition = condition;
        Body = body;
    }
}