using System;
using System.Collections.Generic;

namespace SNEngine.Scripting;

/// <summary>
/// Manages the stack of scopes during code generation.
/// Uses static Current for easy access from all generators without passing context everywhere.
/// </summary>
public class ScopeManager
{
    private static ScopeManager? _current;

    /// <summary>
    /// Current scope manager for the ongoing generation process
    /// </summary>
    public static ScopeManager Current => _current ?? throw new InvalidOperationException(
        "ScopeManager.Current is not initialized. Call ScopeManager.BeginGeneration() first.");

    private readonly Stack<Scope> _scopes = new();

    public Scope CurrentScope => _scopes.Peek();

    /// <summary>
    /// Start new generation session (call before generating a script)
    /// </summary>
    public static void BeginGeneration()
    {
        _current = new ScopeManager();
        _current.PushScope(); // Global scope
    }

    /// <summary>
    /// End generation session
    /// </summary>
    public static void EndGeneration()
    {
        _current = null;
    }

    public void PushScope()
    {
        var parent = _scopes.Count > 0 ? CurrentScope : null;
        _scopes.Push(new Scope(parent));
    }

    public void PopScope()
    {
        if (_scopes.Count <= 1)
            throw new InvalidOperationException("Cannot pop the global scope.");

        _scopes.Pop();
    }

    public void Declare(string name, SymbolKind kind = SymbolKind.Variable)
    {
        CurrentScope.Declare(name, kind);
    }

    public bool IsLocal(string name)
    {
        return CurrentScope.IsLocal(name);
    }

    public Symbol? Resolve(string name)
    {
        return CurrentScope.Resolve(name);
    }
}