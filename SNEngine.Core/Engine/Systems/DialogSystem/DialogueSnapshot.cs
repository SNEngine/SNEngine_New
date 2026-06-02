namespace SNEngine.Core.Engine.Systems.DialogSystem;

/// <summary>
/// Data transfer object for the current dialogue state sent to JS.
/// </summary>
public readonly struct DialogueSnapshot
{
    public string Speaker { get; init; }
    public string Text { get; init; }
    public string FullText { get; init; }
    public string Color { get; init; }
    public bool Visible { get; init; }
    public bool IsComplete { get; init; }
}
