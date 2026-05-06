using MrKWatkins.Ast;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// Base class for all .sn AST nodes.
/// </summary>
public abstract class AstNode : Node<AstNode>
{
    protected AstNode() : base() { }
}