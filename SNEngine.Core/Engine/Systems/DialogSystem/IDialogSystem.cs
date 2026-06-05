using System;
using System.Threading.Tasks;
using SNEngine.Core.Input;

namespace SNEngine.Core.Engine.Systems.DialogSystem;

/// <summary>
/// Unified interface for dialog systems (speech, thoughts, narration, etc.).
/// 
/// Different visual presentations (standard bottom box vs full-screen black "thoughts")
/// can be driven by the same core logic but routed to different IDialogPrinter implementations
/// or different runtime channels (window.SNEngine.runtime.dialog vs .thought).
/// 
/// This avoids duplication between similar text-output systems.
/// </summary>
public interface IDialogSystem : ISystem
{
    string Speaker { get; }
    string DisplayedText { get; }
    string FullText { get; }
    string Color { get; }
    bool IsVisible { get; }
    bool IsTypingComplete { get; }

    /// <summary>
    /// The active dialog presentation type. Used by printers / HTML to choose style.
    /// Common values: "dialog", "thought".
    /// </summary>
    string Type { get; }

    void SayInternal(string speaker, string text, string? color = null, float msPerChar = 30f, string type = "dialog");
    Task SayAsync(string speaker, string text, string? color = null, float msPerChar = 30f, string type = "dialog");

    void Clear();
    void CompleteCurrentLine();
    void Advance();

    DialogueSnapshot GetSnapshot();
}