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

    // Присваивание
    public void Set(object newValue) => Value = newValue;

    // Получение типобезопасно
    public int AsInt() => Convert.ToInt32(Value);
    public double AsDouble() => Convert.ToDouble(Value);
    public string AsString() => Value?.ToString() ?? "";
    public bool AsBool() => Convert.ToBoolean(Value);

    // Арифметика
    public static SNVariable operator +(SNVariable a, SNVariable b)
    {
        if (a.Value is string || b.Value is string)
            return new SNVariable(a.AsString() + b.AsString());

        if (a.Value is double || b.Value is double)
            return new SNVariable(a.AsDouble() + b.AsDouble());

        return new SNVariable(a.AsInt() + b.AsInt());
    }

    public static SNVariable operator -(SNVariable a, SNVariable b)
        => new SNVariable(a.AsDouble() - b.AsDouble());

    public static SNVariable operator *(SNVariable a, SNVariable b)
        => new SNVariable(a.AsDouble() * b.AsDouble());

    public static SNVariable operator /(SNVariable a, SNVariable b)
        => new SNVariable(a.AsDouble() / b.AsDouble());

    public static SNVariable operator +(SNVariable a, object b) => a + new SNVariable(b);
    public static SNVariable operator -(SNVariable a, object b) => a - new SNVariable(b);
    public static SNVariable operator *(SNVariable a, object b) => a * new SNVariable(b);
    public static SNVariable operator /(SNVariable a, object b) => a / new SNVariable(b);

    public override string ToString() => Value?.ToString() ?? "null";
}