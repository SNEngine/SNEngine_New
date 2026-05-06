namespace SNEngine.Scripting;

/// <summary>
/// Attribute to mark command classes
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SnCommandAttribute : Attribute
{
    public string Keyword { get; }
    public SnCommandAttribute(string keyword) => Keyword = keyword;
}