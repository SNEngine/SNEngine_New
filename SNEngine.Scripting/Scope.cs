using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SNEngine.Scripting;

/// <summary>
/// Represents a single lexical scope in the generated script.
/// Supports nested scopes (for, functions, etc.).
/// </summary>
public class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Parent scope (null for global scope)
    /// </summary>
    public Scope? Parent { get; }

    public Scope(Scope? parent = null)
    {
        Parent = parent;
    }

    /// <summary>
    /// Declare a new symbol in current scope
    /// </summary>
    public void Declare(string name, SymbolKind kind = SymbolKind.Variable)
    {
        if (_symbols.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' is already declared in this scope.");

        _symbols[name] = new Symbol(name, kind);
    }

    /// <summary>
    /// Check if symbol exists in current scope or any parent scope
    /// </summary>
    public bool IsLocal(string name)
    {
        return _symbols.ContainsKey(name) || (Parent?.IsLocal(name) ?? false);
    }

    /// <summary>
    /// Check if symbol was declared exactly in this scope (not in parent)
    /// </summary>
    public bool IsDeclaredInCurrentScope(string name) => _symbols.ContainsKey(name);

    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;

        return Parent?.Resolve(name);
    }
}