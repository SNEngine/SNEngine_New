using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// Jump To Chapter1
/// Jumps to another SNScript scene by creating its instance.
/// </summary>
[SnCommand("Jump To")]
public sealed class JumpToCommandNode : CommandNode, IParsableCommand
{
    public string TargetScene { get; }

    public JumpToCommandNode(string targetScene)
    {
        TargetScene = targetScene;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        Try(String("Jump To")
            .Before(SkipWhitespaces)
            .Then(CommonParsers.Identifier)
            .Select(name => new JumpToCommandNode(name))
            .Cast<CommandNode>());
}