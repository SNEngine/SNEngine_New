using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// Get String from age
/// Get String from 46
/// Get String from playerHealth + 10
/// </summary>
[SnCommand("Get String from")]
public sealed class GetStringFromCommandNode : CommandNode, IParsableCommand
{
    public string Expression { get; }

    public GetStringFromCommandNode(string expression)
    {
        Expression = expression.Trim();
    }

    public static Parser<char, CommandNode> Parser { get; } =
        Try(
            String("Get String from")
                .Before(SkipWhitespaces)
                .Then(AnyCharExcept('\n', '\r').ManyString())
                .Select(expr => (CommandNode)new GetStringFromCommandNode(expr))
        );
}