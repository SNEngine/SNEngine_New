namespace SNEngine.Scripting.Ast;

public sealed class ElseIfClause
{
    public string Condition { get; }
    public IReadOnlyList<CommandNode> Body { get; }

    public ElseIfClause(string condition, IReadOnlyList<CommandNode> body)
    {
        Condition = condition;
        Body = body;
    }
}