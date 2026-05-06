namespace SNEngine.Scripting.Ast;

/// <summary>
/// Root of .sn script
/// </summary>
public sealed class ScriptNode : AstNode
{
    public string? SceneName { get; }
    public IReadOnlyList<CommandNode> Commands { get; }

    public ScriptNode(string? sceneName, IReadOnlyList<CommandNode> commands)
    {
        SceneName = sceneName;
        Commands = commands;
    }
}