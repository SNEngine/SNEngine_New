using System;

namespace SNEngine.API;

/// <summary>
/// Universal variable for .sn scripting system.
/// Supports int, double, string, bool and basic operations.
/// </summary>
public class SNVariable
{
    public object Value { get; private set; }

    public SNVariable(object value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void Set(object newValue) => Value = newValue;

    // === Типобезопасные конвертеры ===
    public int AsInt() => Convert.ToInt32(Value);
    public long AsLong() => Convert.ToInt64(Value);
    public float AsFloat() => Convert.ToSingle(Value);
    public double AsDouble() => Convert.ToDouble(Value);
    public decimal AsDecimal() => Convert.ToDecimal(Value);
    public bool AsBool() => Convert.ToBoolean(Value);
    public string AsString() => Value?.ToString() ?? "";

    // === Операторы ===
    public static SNVariable operator +(SNVariable a, SNVariable b)
    {
        if (a.Value is string || b.Value is string)
            return new SNVariable(a.AsString() + b.AsString());

        if (a.Value is double || b.Value is double || a.Value is float || b.Value is float)
            return new SNVariable(a.AsDouble() + b.AsDouble());

        return new SNVariable(a.AsInt() + b.AsInt());
    }

    public static SNVariable operator -(SNVariable a, SNVariable b) => new(a.AsDouble() - b.AsDouble());
    public static SNVariable operator *(SNVariable a, SNVariable b) => new(a.AsDouble() * b.AsDouble());
    public static SNVariable operator /(SNVariable a, SNVariable b) => new(a.AsDouble() / b.AsDouble());

    public static SNVariable operator +(SNVariable a, object b) => a + new SNVariable(b);
    public static SNVariable operator -(SNVariable a, object b) => a - new SNVariable(b);
    public static SNVariable operator *(SNVariable a, object b) => a * new SNVariable(b);
    public static SNVariable operator /(SNVariable a, object b) => a / new SNVariable(b);

    public override string ToString() => Value?.ToString() ?? "null";
}