using System;

namespace SNEngine.Scripting.AssemblyBuilder.Attributes;

/// <summary>
/// Marks a class as a build stage with execution order.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BuildStageAttribute : Attribute
{
    /// <summary>
    /// Execution order (lower numbers run first)
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Optional custom display name for logs
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Main constructor
    /// </summary>
    public BuildStageAttribute(int order, string? name = null)
    {
        Order = order;
        Name = name;
    }
}