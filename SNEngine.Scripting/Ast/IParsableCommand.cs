using Pidgin;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting;

public interface IParsableCommand
{
    static abstract Parser<char, CommandNode> Parser { get; }
}