using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// call functionName(arg1, arg2, ...)
/// Пример: call calculate(10, 20)
///          call showMessage("Hello", playerName)
///          call hello()
/// </summary>
[SnCommand("call")]
public sealed class CallCommandNode : CommandNode, IParsableCommand
{
    public string FunctionName { get; }
    public IReadOnlyList<string> Arguments { get; }

    public CallCommandNode(string functionName, IReadOnlyList<string> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments ?? new List<string>();
    }

    public static Parser<char, CommandNode> Parser { get; } =
        String("call")
            .Before(SkipWhitespaces)
            .Then(CommonParsers.Identifier)
            .Before(SkipWhitespaces)
            .Before(Char('('))
            .Before(SkipWhitespaces)
            .Then(name =>
                OneOf(
                    // Пустые скобки: ()
                    Char(')').Select(_ => (CommandNode)new CallCommandNode(name, new List<string>())),
                    // Аргументы: arg1, arg2, ...
                    AnyCharExcept('\n', '\r', ')')
                        .ManyString()
                        .Before(Char(')'))
                        .Select(args => (CommandNode)new CallCommandNode(
                            name,
                            args.Split(',').Select(a => a.Trim()).ToList()
                        ))
                )
            );
}