using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// continue
/// </summary>
[SnCommand("continue")]
public sealed class ContinueCommandNode : CommandNode, IParsableCommand
{
    public static Parser<char, CommandNode> Parser { get; } =
        String("continue")
            .Select(_ => (CommandNode)new ContinueCommandNode());
}