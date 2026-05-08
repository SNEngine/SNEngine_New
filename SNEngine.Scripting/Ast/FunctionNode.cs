namespace SNEngine.Scripting.Ast;

public sealed class FunctionNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<FunctionParameter> Parameters { get; }
    public string ReturnType { get; }           // ← новое поле
    public IReadOnlyList<CommandNode> Body { get; }

    public FunctionNode(string name,
                        IReadOnlyList<FunctionParameter> parameters,
                        string returnType,
                        IReadOnlyList<CommandNode> body)
    {
        Name = name ?? "";
        Parameters = parameters ?? Array.Empty<FunctionParameter>();
        ReturnType = string.IsNullOrWhiteSpace(returnType) ? "void" : returnType;
        Body = body;
    }
}

public sealed class FunctionParameter
{
    public string Type { get; }
    public string Name { get; }
    public string? DefaultValue { get; }

    public FunctionParameter(string type, string name, string? defaultValue = null)
    {
        Type = type ?? "var";
        Name = name ?? "";
        DefaultValue = defaultValue;
    }
}