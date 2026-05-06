using SNEngine.Scripting.Ast;

public sealed class ScriptNode : AstNode
{
    public string? SceneName { get; }
    public IReadOnlyList<CommandNode> Commands { get; }           // main body
    public IReadOnlyList<FunctionNode> Functions { get; }         // user functions

    public ScriptNode(string? sceneName, IReadOnlyList<CommandNode> commands, IReadOnlyList<FunctionNode> functions)
    {
        SceneName = sceneName;
        Commands = commands;
        Functions = functions;
    }
}