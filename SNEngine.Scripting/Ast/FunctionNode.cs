namespace SNEngine.Scripting.Ast;

/// <summary>
/// Represents a user-defined function: function name() ... endfunc
/// </summary>
public sealed class FunctionNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<CommandNode> Body { get; }
    
    public FunctionNode(string name, IReadOnlyList<CommandNode> body)
    {
        Name = name;
        Body = body;
    }
}