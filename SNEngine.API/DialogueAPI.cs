using SNEngine.Core.Engine;
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
        DialogueSystem.SayInternal(speaker, text, color, msPerChar);
    }

    /// <summary>
    /// Asynchronous Say. The Task completes only after Core has finished the gradual reveal (typewriter).
    /// </summary>
    public static Task SayAsync(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        return DialogueSystem.SayAsync(speaker, text, color, msPerChar);
    }

    /// <summary>
    /// Immediately finish revealing the current line (useful for skipping).
    /// </summary>
    public static void CompleteLine()
    {
        DialogueSystem.CompleteCurrentLine();
    }

    /// <summary>
    /// Clear / hide current dialogue line.
    /// </summary>
    public static void Clear()
    {
        DialogueSystem.Clear();
    }

    /// <summary>
    /// Quick helper: Say using raw display name + color (no character lookup).
    /// </summary>
    public static void SayDirect(string speaker, string text, string color = "#FFFFFF", float msPerChar = 30f)
    {
        DialogueSystem.SayInternal(speaker, text, color, msPerChar);
    }

    /// <summary>
    /// Async version of SayDirect.
    /// </summary>
    public static Task SayDirectAsync(string speaker, string text, string color = "#FFFFFF", float msPerChar = 30f)
    {
        return DialogueSystem.SayAsync(speaker, text, color, msPerChar);
    }
}
