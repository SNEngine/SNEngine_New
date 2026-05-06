using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// Quit
/// Выход из игры (вызывает SNEngine.API.SNEngine.Quit())
/// </summary>
[SnCommand("Quit")]
public sealed class QuitCommandNode : CommandNode, IParsableCommand
{
    public QuitCommandNode() { }

    public static Parser<char, CommandNode> Parser { get; } =
        Try(String("Quit")
            .Before(SkipWhitespaces)
            .Select(_ => new QuitCommandNode())
            .Cast<CommandNode>());
}