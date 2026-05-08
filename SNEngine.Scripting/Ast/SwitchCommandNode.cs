using System.Collections.Generic;

namespace SNEngine.Scripting.Ast;

public sealed class SwitchCommandNode : CommandNode
{
    public string Expression { get; }
    public List<SwitchCaseNode> Cases { get; }
    public List<CommandNode>? DefaultBody { get; }

    public SwitchCommandNode(string expression,
                             List<SwitchCaseNode> cases,
                             List<CommandNode>? defaultBody = null)
    {
        Expression = expression;
        Cases = cases;
        DefaultBody = defaultBody;
    }
}

public sealed class SwitchCaseNode
{
    public string Value { get; }
    public List<CommandNode> Body { get; }

    public SwitchCaseNode(string value, List<CommandNode> body)
    {
        Value = value;
        Body = body;
    }
}