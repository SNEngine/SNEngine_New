using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// call hello()
/// call calculateDamage()
/// </summary>
[SnCommand("call")]
public sealed class CallCommandNode : CommandNode, IParsableCommand
{
    public string FunctionName { get; }

    public CallCommandNode(string functionName)
    {
        FunctionName = functionName;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        String("call")
            .Before(SkipWhitespaces)
            .Then(CommonParsers.Identifier)
            .Before(SkipWhitespaces)
            .Before(Char('('))
            .Before(SkipWhitespaces)
            .Before(Char(')'))
            .Select(name => (CommandNode)new CallCommandNode(name));
}