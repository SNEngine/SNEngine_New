namespace SNEngine.Core.Engine.Systems.DialogSystem;

/// <summary>
/// Aggregated runtime data snapshot.
/// </summary>
public readonly struct RuntimeSnapshot
{
    public double Fps { get; init; }
    public DialogueSnapshot Dialogue { get; init; }
}