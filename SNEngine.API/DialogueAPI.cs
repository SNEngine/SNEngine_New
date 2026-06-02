using SNEngine.Core.Engine;
using SNEngine.Core.Engine.Systems.DialogSystem;
using System;
using System.Threading.Tasks;

namespace SNEngine.API;

/// <summary>
/// High-level dialogue facade. Delegates to the Core DialogueSystem which handles
/// Say + gradual (typewriter) text reveal.
/// </summary>
public static class DialogueAPI
{

    /// <summary>
    /// Fire-and-forget version (starts the line but does not wait).
    /// For proper waiting until typing finishes, use <see cref="SayAsync"/>.
    /// </summary>
    public static void Say(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        SNEngineHost.Current.GetSystem<DialogueSystem>().SayInternal(speaker, text, color, msPerChar);
    }

    /// <summary>
    /// Asynchronous Say. The Task completes only after the player clicks to advance
    /// (the typewriter finishes or is skipped, then an explicit mouse click confirms the line).
    /// </summary>
    public static Task SayAsync(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        return SNEngineHost.Current.GetSystem<DialogueSystem>().SayAsync(speaker, text, color, msPerChar);
    }

    /// <summary>
    /// Immediately finish revealing the current line (skips the typewriter effect).
    /// Does not advance to the next line — the player must still click to proceed.
    /// </summary>
    public static void CompleteLine()
    {
        SNEngineHost.Current.GetSystem<DialogueSystem>().CompleteCurrentLine();
    }

    /// <summary>
    /// Signals that the player clicked to advance the current dialogue line.
    /// If the text is still typing, this skips the effect. If already complete (full text shown),
    /// it unblocks any waiting SayAsync **and auto-hides the dialog box**.
    /// The hide is performed by clearing the visible state so the HTML sees "no data" (dialog = null)
    /// and hides the container via its normal logic. This makes two consecutive <c>await Say(...)</c>
    /// calls look like a natural continuation instead of the old line lingering.
    /// Called automatically from the dialog HTML on click, and can also be called from C#.
    /// </summary>
    public static void Advance()
    {
        SNEngineHost.Current.GetSystem<DialogueSystem>().Advance();
    }

    /// <summary>
    /// Clear / hide current dialogue line.
    /// </summary>
    public static void Clear()
    {
        SNEngineHost.Current.GetSystem<DialogueSystem>().Clear();
    }

    /// <summary>
    /// Quick helper: Say using raw display name + color (no character lookup).
    /// </summary>
    public static void SayDirect(string speaker, string text, string color = "#FFFFFF", float msPerChar = 30f)
    {
        SNEngineHost.Current.GetSystem<DialogueSystem>().SayInternal(speaker, text, color, msPerChar);
    }

    /// <summary>
    /// Async version of SayDirect.
    /// </summary>
    public static Task SayDirectAsync(string speaker, string text, string color = "#FFFFFF", float msPerChar = 30f)
    {
        return SNEngineHost.Current.GetSystem<DialogueSystem>().SayAsync(speaker, text, color, msPerChar);
    }
}
