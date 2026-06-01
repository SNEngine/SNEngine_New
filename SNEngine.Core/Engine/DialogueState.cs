using System;

namespace SNEngine.Core.Engine;

/// <summary>
/// [Obsolete] Legacy compatibility shim.
/// 
/// All new code should use <see cref="DialogueSystem"/> directly.
/// This class now forwards to the real Core dialogue system (with typewriter support).
/// 
/// Will be removed in a future version.
/// </summary>
[Obsolete("Use DialogueSystem instead. This is a temporary compatibility layer.")]
public static class DialogueState
{
    [Obsolete("Use DialogueSystem.Say instead.")]
    public static void Set(string speaker, string text, string color, bool visible)
    {
        if (visible)
        {
            DialogueSystem.SayInternal(speaker, text, color);
        }
        else
        {
            DialogueSystem.Clear();
        }
    }

    [Obsolete("Use DialogueSystem.Clear instead.")]
    public static void Clear()
    {
        DialogueSystem.Clear();
    }

    [Obsolete("Use DialogueSystem.GetSnapshot instead.")]
    public static (string Speaker, string Text, string Color, bool Visible) GetCurrent()
    {
        var snap = DialogueSystem.GetSnapshot();
        return (snap.Speaker, snap.Text, snap.Color, snap.Visible);
    }

}
