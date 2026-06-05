namespace SNEngine.Core.Engine.Systems.DialogSystem;

/// <summary>
/// Abstraction for a "printer" that knows how to visually present a dialog line
/// in a specific style (e.g. classic VN dialog box with speaker, or full-screen
/// black background for character's inner thoughts/monologue).
/// 
/// The core IDialogSystem handles typewriter logic, state, input/advance, async waiting.
/// Printers (C# bridge + specific JS/HTML) consume snapshots or direct calls and
/// render according to snapshot.Type (or their own registration).
/// 
/// This allows unifying similar "text output" features without code duplication.
/// </summary>
public interface IDialogPrinter
{
    /// <summary>
    /// The unique id/channel this printer listens to (e.g. "dialog", "thought").
    /// Used to route state from IDialogSystem(s).
    /// </summary>
    string Id { get; }

    void Print(DialogueSnapshot snapshot);
    void Clear();
}