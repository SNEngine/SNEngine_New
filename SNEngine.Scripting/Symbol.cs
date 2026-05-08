namespace SNEngine.Scripting;

/// <summary>
/// Represents a declared symbol (variable, loop variable, function, etc.)
/// </summary>
public record Symbol(string Name, SymbolKind Kind);

public enum SymbolKind
{
    Variable,
    LoopVariable,
    Function,
    Parameter
}