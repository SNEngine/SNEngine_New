using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// break
/// </summary>
[SnCommand("break")]
public sealed class BreakCommandNode : CommandNode, IParsableCommand
{
    public static Parser<char, CommandNode> Parser { get; } =
        String("break")
            .Select(_ => (CommandNode)new BreakCommandNode());
}