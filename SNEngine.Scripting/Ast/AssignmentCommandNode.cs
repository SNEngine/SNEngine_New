using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// myVar = 42
/// myVar = "text"
/// myVar = myVar2 + 10
/// </summary>
[SnCommand("=")]
public sealed class AssignmentCommandNode : CommandNode, IParsableCommand
{
    public string VariableName { get; }
    public string ValueExpression { get; }

    public AssignmentCommandNode(string variableName, string valueExpression)
    {
        VariableName = variableName;
        ValueExpression = valueExpression;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        CommonParsers.Identifier
            .Before(SkipWhitespaces)
            .Before(Char('='))
            .Before(SkipWhitespaces)
            .Then(name =>
                AnyCharExcept('\n', '\r')
                    .ManyString()
                    .Select(value => (CommandNode)new AssignmentCommandNode(name, value.Trim()))
            );
}