using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// if condition then ... else ... endif
/// </summary>
[SnCommand("if")]
public sealed class IfCommandNode : CommandNode, IParsableCommand
{
    public string Condition { get; }
    public IReadOnlyList<CommandNode> ThenBody { get; }
    public IReadOnlyList<CommandNode> ElseBody { get; }

    public IfCommandNode(string condition,
                         IReadOnlyList<CommandNode> thenBody,
                         IReadOnlyList<CommandNode> elseBody)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        String("if")
            .Before(SkipWhitespaces)
            .Then(AnyCharExcept('\n', '\r').ManyString())
            .Before(SkipWhitespaces)
            .Before(String("then"))
            .Before(SkipWhitespaces)
            .Before(EndOfLine.IgnoreResult())           // ← исправленная строка
            .Select(condition => (CommandNode)new IfCommandNode(
                condition.Trim(),
                new List<CommandNode>(),
                new List<CommandNode>()));
}