using SNEngine.Core.Engine;
using System;

namespace SNEngine.API;

/// <summary>
/// Simple dialogue / "Say" system.
/// Writes to the neutral DialogueState (Core) so that every UltralightHtmlElement
/// can push the data into window.SNEngine.runtime.dialog via SNEngineRuntimeBridge.
/// The dialog HTML (dialog/index.html) polls the window object and auto-hides when empty.
/// </summary>
public static class DialogueAPI
{
    /// <summary>
    /// Show a dialogue line. Speaker name, text and optional color (for UI).
    /// </summary>
    public static void Say(string speaker, string text, string? color = null)
    {
        string finalColor = string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color!;
        DialogueState.Set(speaker ?? string.Empty, text ?? string.Empty, finalColor, true);

        Console.WriteLine($"[DialogueAPI] Say: {speaker} → {text}");
    }

    /// <summary>
    /// Clear / hide current dialogue line.
    /// </summary>
    public static void Clear()
    {
        DialogueState.Clear();
        Console.WriteLine("[DialogueAPI] Dialog cleared");
    }

    /// <summary>
    /// Quick helper: Say using raw display name + color (no character lookup).
    /// </summary>
    public static void SayDirect(string speaker, string text, string color = "#FFFFFF")
    {
        Say(speaker, text, color);
    }
}
