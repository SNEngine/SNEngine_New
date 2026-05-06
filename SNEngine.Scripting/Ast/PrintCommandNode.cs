using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// print "Hello world"
/// print myVar2
/// print "Health: " + playerHealth
/// </summary>
[SnCommand("print")]
public sealed class PrintCommandNode : CommandNode, IParsableCommand
{
    public string Message { get; }

    public PrintCommandNode(string message)
    {
        Message = message;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        String("print")
            .Before(SkipWhitespaces)
            .Then(AnyCharExcept('\n', '\r').ManyString())
            .Select(msg => (CommandNode)new PrintCommandNode(msg.Trim()));
}