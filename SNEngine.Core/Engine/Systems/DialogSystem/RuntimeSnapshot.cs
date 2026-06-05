using SNEngine.Core.Engine.Systems.FpsSystem;

namespace SNEngine.Core.Engine.Systems.DialogSystem;

/// <summary>
/// Aggregated runtime data snapshot.
/// </summary>
public readonly struct RuntimeSnapshot
{
    public double Fps { get; init; }
    public FpsSnapshot FpsState { get; init; }
    public DialogueSnapshot Dialogue { get; init; }

    /// <summary>
    /// Snapshot for the on-screen / full-screen dialog variant (e.g. character thoughts, black bg full screen).
    /// Uses same DialogueSnapshot shape for unification via IDialogSystem/IDialogPrinter.
    /// The corresponding HTML printer listens to a separate runtime key.
    /// </summary>
    public DialogueSnapshot OnScreenDialogue { get; init; }
}