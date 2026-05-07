using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Scope;

/// <summary>
/// Одна область видимости (lexical scope).
/// Поддерживает локальные переменные и вложенные скопы.
/// </summary>
public class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
    public Scope? Parent { get; }

    public Scope(Scope? parent = null)
    {
        Parent = parent;
    }

    /// <summary>
    /// Добавляет переменную в текущую область видимости.
    /// </summary>
    public void Declare(string name, SymbolKind kind = SymbolKind.Variable)
    {
        if (_symbols.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' is already declared in this scope");

        _symbols[name] = new Symbol(name, kind);
    }

    /// <summary>
    /// Проверяет, объявлена ли переменная в этой области или родительских.
    /// </summary>
    public bool IsDeclared(string name)
    {
        if (_symbols.ContainsKey(name))
            return true;

        return Parent?.IsDeclared(name) ?? false;
    }

    /// <summary>
    /// Проверяет, локальная ли это переменная в текущей области.
    /// </summary>
    public bool IsLocal(string name) => _symbols.ContainsKey(name);

    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;

        return Parent?.Resolve(name);
    }
}

public record Symbol(string Name, SymbolKind Kind);

public enum SymbolKind
{
    Variable,
    LoopVariable,
    Function
}