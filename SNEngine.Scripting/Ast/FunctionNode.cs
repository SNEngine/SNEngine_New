namespace SNEngine.Scripting.Ast;

public sealed class FunctionNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<FunctionParameter> Parameters { get; }
    public IReadOnlyList<CommandNode> Body { get; }

    public FunctionNode(string name, IReadOnlyList<FunctionParameter> parameters, IReadOnlyList<CommandNode> body)
    {
        Name = name ?? "";
        Parameters = parameters ?? Array.Empty<FunctionParameter>();
        Body = body;
    }
}

public sealed class FunctionParameter
{
    public string Type { get; }
    public string Name { get; }
    public string? DefaultValue { get; }   // ← добавлено

    public FunctionParameter(string type, string name, string? defaultValue = null)
    {
        Type = type ?? "var";
        Name = name ?? "";
        DefaultValue = defaultValue;
    }
}