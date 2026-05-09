using SNEngine.Scripting.Ast;
using SNEngine.Scripting.AST;

namespace SNEngine.Scripting.AST;

/// <summary>
/// Represents a raw C# code block inserted via native ... endnative
/// </summary>
public sealed class NativeCommandNode : CommandNode
{
    /// <summary>
    /// Raw C# source code inside the native block
    /// </summary>
    public string RawCSharpCode { get; }

    /// <summary>
    /// Source line number where the native block starts
    /// </summary>
    public int Line { get; }

    public NativeCommandNode(string rawCode, int line)
    {
        RawCSharpCode = rawCode;
        Line = line;
    }

    public override string ToString() => $"NativeBlock (Line {Line}, {RawCSharpCode.Length} characters)";
}