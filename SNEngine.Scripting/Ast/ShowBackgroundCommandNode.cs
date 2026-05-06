using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// Show Background class_bg
/// </summary>
[SnCommand("Show Background")]
public sealed class ShowBackgroundCommandNode : CommandNode, IParsableCommand
{
    public string BackgroundName { get; }

    public ShowBackgroundCommandNode(string backgroundName)
    {
        BackgroundName = backgroundName;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        Try(String("Show")
            .Before(SkipWhitespaces)
            .Then(String("Background"))
            .Before(SkipWhitespaces)
            .Then(CommonParsers.Identifier)
            .Select(name => new ShowBackgroundCommandNode(name))
            .Cast<CommandNode>());
}