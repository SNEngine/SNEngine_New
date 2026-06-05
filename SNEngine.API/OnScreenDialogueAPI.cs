using SNEngine.Core.Engine;
using SNEngine.Core.Engine.Systems.DialogOnScreenSystem;
using System;
using System.Threading.Tasks;

namespace SNEngine.API;

/// <summary>
/// API for the full-screen / on-screen dialog variant (thoughts, internal monologue).
/// Separate screen (ui/dialog-onscreen) + separate system for clean separation.
/// 
/// Unifies with classic via IDialogSystem (both produce compatible snapshots with .Type).
/// </summary>
public static class OnScreenDialogueAPI
{
    public static void Say(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        SNEngineHost.Current.GetSystem<DialogOnScreenSystem>()?.SayInternal(speaker, text, color, msPerChar);
    }

    public static Task SayAsync(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        var sys = SNEngineHost.Current.GetSystem<DialogOnScreenSystem>();
        return sys?.SayAsync(speaker, text, color, msPerChar) ?? Task.CompletedTask;
    }

    public static void CompleteLine()
    {
        SNEngineHost.Current.GetSystem<DialogOnScreenSystem>()?.CompleteCurrentLine();
    }

    public static void Advance()
    {
        SNEngineHost.Current.GetSystem<DialogOnScreenSystem>()?.Advance();
    }

    public static void Clear()
    {
        SNEngineHost.Current.GetSystem<DialogOnScreenSystem>()?.Clear();
    }

    public static void SayDirect(string speaker, string text, string color = "#FFFFFF", float msPerChar = 30f)
    {
        SNEngineHost.Current.GetSystem<DialogOnScreenSystem>()?.SayInternal(speaker, text, color, msPerChar);
    }

    public static Task SayDirectAsync(string speaker, string text, string color = "#FFFFFF", float msPerChar = 30f)
    {
        var sys = SNEngineHost.Current.GetSystem<DialogOnScreenSystem>();
        return sys?.SayAsync(speaker, text, color, msPerChar) ?? Task.CompletedTask;
    }

    // Convenience for "thoughts" style (the primary use of on-screen dialog).
    // This version returns Task so it can be awaited for completion (after player clicks to advance),
    // without changing the method name from "Think".
    public static Task Think(string text, string? color = null, float msPerChar = 40f)
    {
        return SayAsync(string.Empty, text, color, msPerChar);
    }

    public static Task ThinkAsync(string text, string? color = null, float msPerChar = 40f)
    {
        return SayAsync(string.Empty, text, color, msPerChar);
    }
}
