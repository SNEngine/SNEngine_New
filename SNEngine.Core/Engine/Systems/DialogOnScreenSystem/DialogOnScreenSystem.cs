using System;
using System.Text;
using System.Threading.Tasks;
using SNEngine.Core;
using SNEngine.Core.Input;
using SNEngine.Core.Engine.Systems.DialogSystem;

namespace SNEngine.Core.Engine.Systems.DialogOnScreenSystem;

/// <summary>
/// On-screen / full-screen dialog system (e.g. character's thoughts, internal monologue).
/// Black background + text filling the screen.
/// 
/// Separate from the classic DialogueSystem (bottom box with speaker) as requested.
/// 
/// Unifies with the classic via IDialogSystem / IDialogPrinter contracts:
/// both produce DialogueSnapshot (with .Type = "onscreen" or "dialog"),
/// support same text reveal / advance / async wait semantics.
/// The corresponding HTML printer (separate folder) renders full-screen style.
/// </summary>
public class DialogOnScreenSystem : ISystem, IDialogSystem
{
    private string _speaker = string.Empty;
    private string _fullText = string.Empty;
    private string _color = "#FFFFFF";
    private bool _visible;

    private float _msPerChar = 30f;
    private float _timeSinceStart;
    private int _revealedChars;
    private bool _isComplete;

    private readonly StringBuilder _displayBuilder = new StringBuilder(512);
    private TaskCompletionSource<bool>? _currentLineTcs;

    // ISystem / IDialogSystem
    public string SystemName => "DialogOnScreenSystem";

    public string Speaker => _speaker;
    public string DisplayedText => _displayBuilder.ToString();
    public string FullText => _fullText;
    public string Color => _color;
    public bool IsVisible => _visible;
    public bool IsTypingComplete => _isComplete || _revealedChars >= _fullText.Length;

    /// <summary>
    /// For this on-screen variant, always "onscreen" (or "thought").
    /// Allows IDialogPrinters to choose the full-screen black style.
    /// </summary>
    public string Type => "onscreen";

    public void RegisterWithInputRouter(InputRouter inputRouter)
    {
        inputRouter?.RegisterSystem(this);
    }

    // === Public API (similar to classic for unification) ===
    public void SayInternal(string speaker, string text, string? color = null, float msPerChar = 30f, string type = "onscreen")
    {
        CompleteCurrentTcs(false);

        _speaker = speaker ?? string.Empty;
        _fullText = text ?? string.Empty;
        _color = string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color!;
        _visible = true;

        _msPerChar = msPerChar <= 0 ? 1f : msPerChar;
        _timeSinceStart = 0f;
        _revealedChars = 0;
        _isComplete = false;

        _displayBuilder.Clear();
        _currentLineTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Clear()
    {
        CompleteCurrentTcs(false);
        HideCurrentDialogueInternal();
        Debug.Log("[DialogOnScreenSystem] Cleared");
    }

    private void HideCurrentDialogueInternal()
    {
        _visible = false;
        _speaker = string.Empty;
        _fullText = string.Empty;
        _timeSinceStart = 0f;
        _revealedChars = 0;
        _isComplete = false;
        _color = "#FFFFFF";
        _displayBuilder.Clear();
    }

    private void CompleteCurrentTcs(bool result)
    {
        if (_currentLineTcs != null)
        {
            _currentLineTcs.TrySetResult(result);
            _currentLineTcs = null;
        }
    }

    public Task WaitForCurrentLineAsync()
    {
        if (!_visible)
            return Task.CompletedTask;

        _currentLineTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _currentLineTcs.Task;
    }

    public async Task SayAsync(string speaker, string text, string? color = null, float msPerChar = 30f, string type = "onscreen")
    {
        SayInternal(speaker, text, color, msPerChar, type);
        await WaitForCurrentLineAsync();
    }

    public void CompleteCurrentLine()
    {
        if (!_visible || string.IsNullOrEmpty(_fullText)) return;

        _revealedChars = _fullText.Length;
        _isComplete = true;
        _timeSinceStart = float.MaxValue;

        _displayBuilder.Clear();
        _displayBuilder.Append(_fullText);
    }

    private float _lastAdvanceRequestTime;

    public void Advance()
    {
        if (!_visible) return;

        float now = Engine.Time.ElapsedTime;
        if (now - _lastAdvanceRequestTime < 0.08f) return;
        _lastAdvanceRequestTime = now;

        if (!IsTypingComplete)
            CompleteCurrentLine();
        else
        {
            CompleteCurrentTcs(true);
            HideCurrentDialogueInternal();
        }
    }

    public void Update(double deltaTime = 0)
    {
        if (!_visible || _isComplete || string.IsNullOrEmpty(_fullText))
            return;

        float dt = deltaTime > 0
            ? (float)deltaTime
            : (Engine.Time.SmoothDeltaTime > 0 ? Engine.Time.SmoothDeltaTime : 0.016f);

        _timeSinceStart += dt;

        float secondsPerChar = _msPerChar / 1000f;
        int targetRevealed = (int)(_timeSinceStart / secondsPerChar);

        const int MaxCharsPerFrame = 3;
        int charsToReveal = Math.Min(targetRevealed - _revealedChars, MaxCharsPerFrame);
        charsToReveal = Math.Max(0, charsToReveal);

        for (int i = 0; i < charsToReveal && _revealedChars < _fullText.Length; i++)
        {
            _displayBuilder.Append(_fullText[_revealedChars]);
            _revealedChars++;
        }

        if (_revealedChars >= _fullText.Length)
        {
            _isComplete = true;
            if (_displayBuilder.Length < _fullText.Length)
            {
                _displayBuilder.Clear();
                _displayBuilder.Append(_fullText);
            }
        }
    }

    public DialogueSnapshot GetSnapshot()
    {
        return new DialogueSnapshot
        {
            Speaker = _speaker,
            Text = DisplayedText,
            FullText = _fullText,
            Color = _color,
            Visible = _visible,
            IsComplete = IsTypingComplete,
            Type = Type
        };
    }

    // ISystem explicit implementation
    void ISystem.OnMouseButtonDown(MouseButton button)
    {
        if (button == MouseButton.Left && IsVisible)
            Advance();
    }

    void ISystem.Update(double deltaTime) => Update(deltaTime);

    void ISystem.OnKeyDown(Key key) { /* not used */ }
    void ISystem.OnKeyUp(Key key) { /* not used */ }
}
