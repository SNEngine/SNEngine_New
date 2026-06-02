namespace SNEngine.Core.Engine.Systems.FpsSystem;

/// <summary>
/// Data transfer object for the current FPS state sent to JS.
/// Allows the HTML to decide its own visibility (like dialog).
/// </summary>
public readonly struct FpsSnapshot
{
    public double Value { get; init; }
    public bool Visible { get; init; }
}
