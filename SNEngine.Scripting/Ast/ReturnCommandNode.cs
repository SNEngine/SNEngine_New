using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// return
/// return 42
/// return playerHealth + 10
/// return          ← теперь работает
/// </summary>
[SnCommand("return")]
public sealed class ReturnCommandNode : CommandNode, IParsableCommand
{
    public string? ReturnValue { get; }

    public ReturnCommandNode(string? returnValue = null)
    {
        ReturnValue = returnValue;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        String("return")
            .Before(SkipWhitespaces)
            .Then(
                OneOf(
                    // Есть значение после return
                    Try(AnyCharExcept('\n', '\r')
                        .AtLeastOnceString()
                        .Select(value => (CommandNode)new ReturnCommandNode(value.Trim()))),

                    // Просто "return" без значения
                    Return((CommandNode)new ReturnCommandNode(null))
                )
            );
}