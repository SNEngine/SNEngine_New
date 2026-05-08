using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// local variableName = expression
/// Пример: local damage = i * 2.5
/// </summary>
[SnCommand("local")]
public sealed class LocalAssignmentCommandNode : CommandNode, IParsableCommand
{
    public string VariableName { get; }
    public string ValueExpression { get; }

    public LocalAssignmentCommandNode(string variableName, string valueExpression)
    {
        VariableName = variableName.Trim();
        ValueExpression = valueExpression.Trim();
    }

    public static Parser<char, CommandNode> Parser { get; } =
        CIString("local")
            .Before(SkipWhitespaces)
            .Then(_ =>
                CommonParsers.Identifier
                    .Before(SkipWhitespaces)
                    .Before(Char('='))
                    .Before(SkipWhitespaces)
                    .Then(name =>
                        AnyCharExcept('\n', '\r')
                            .ManyString()
                            .Select(value => (CommandNode)new LocalAssignmentCommandNode(name, value.Trim()))
                    )
            );
}