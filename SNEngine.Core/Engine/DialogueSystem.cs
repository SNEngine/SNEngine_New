using System;
using System.Text;
using System.Threading.Tasks;

namespace SNEngine.Core.Engine;

/// <summary>
/// Core dialogue / "Say" system with built-in typewriter (gradual text reveal) using StringBuilder.
/// 
/// This is the single source of truth for current dialogue line.
/// It is updated every frame from SNEngineHost.
/// 
/// The system produces a snapshot that gets pushed into every active HTML view's
/// window.SNEngine.runtime.dialog via the runtime bridges.
/// 
/// UI layer (UltralightHtmlElement) no longer hardcodes any dialogue or FPS knowledge.
/// </summary>
public static class DialogueSystem
{
    private static string _speaker = string.Empty;
    private static string _fullText = string.Empty;
    private static string _color = "#FFFFFF";
    private static bool _visible;

    // Typewriter state - using milliseconds per character for smooth typing
    private static float _msPerChar = 30f;          // milliseconds between characters
    private static float _timeSinceStart;           // total time since this line started (more stable than pure accumulator)
    private static int _revealedChars;              // how many characters have been revealed so far
    private static bool _isComplete;

    // Real gradual building with StringBuilder (as requested)
    private static readonly StringBuilder _displayBuilder = new StringBuilder(512);

    // Async completion support
    private static TaskCompletionSource<bool>? _currentLineTcs;

    /// <summary>
    /// Current speaker name (for display).
    /// </summary>
    public static string Speaker => _speaker;

    /// <summary>
    /// The portion of the text that has been revealed so far.
    /// Built incrementally using StringBuilder for proper gradual printing.
    /// </summary>
    public static string DisplayedText => _displayBuilder.ToString();

    /// <summary>
    /// Full original text of the current line.
    /// </summary>
    public static string FullText => _fullText;

    public static string Color => _color;
    public static bool IsVisible => _visible;
    public static bool IsTypingComplete => _isComplete || _revealedChars >= _fullText.Length;

    // NOTE: This is the low-level entry point now that the public synchronous Say was removed.
    // Prefer using DialogueAPI.Say or DialogueAPI.SayAsync in most cases.
    public static void SayInternal(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        // Interrupt previous line
        CompleteCurrentTcs(false);

        _speaker = speaker ?? string.Empty;
        _fullText = text ?? string.Empty;
        _color = string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color!;
        _visible = true;

        // Store speed in milliseconds per character (more convenient for tuning)
        _msPerChar = msPerChar <= 0 ? 1f : msPerChar; // 1ms = very fast, practically instant
        _timeSinceStart = 0f;
        _revealedChars = 0;
        _isComplete = false;

        // Reset the builder for fresh gradual output
        _displayBuilder.Clear();

        // Create new async waiter
        _currentLineTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        float effectiveCps = 1000f / _msPerChar;
    }

    /// <summary>
    /// Hides and clears the current dialogue line.
    /// Any pending async waiter is completed as canceled/interrupted.
    /// </summary>
    public static void Clear()
    {
        CompleteCurrentTcs(false);

        _visible = false;
        _speaker = string.Empty;
        _fullText = string.Empty;
        _timeSinceStart = 0f;
        _revealedChars = 0;
        _isComplete = false;
        _color = "#FFFFFF";

        _displayBuilder.Clear();

        Debug.Log("[DialogueSystem] Cleared");
    }

    private static void CompleteCurrentTcs(bool result)
    {
        if (_currentLineTcs != null)
        {
            try
            {
                _currentLineTcs.TrySetResult(result);
            }
            catch { /* ignore */ }

            _currentLineTcs = null;
        }
    }

    /// <summary>
    /// Returns a task that completes when the current dialogue line finishes typing
    /// (or immediately if no line is active or typing is already complete).
    /// </summary>
    public static Task WaitForCurrentLineAsync()
    {
        if (!_visible || IsTypingComplete)
            return Task.CompletedTask;

        if (_currentLineTcs == null)
        {
            // Defensive: create one if somehow missing
            _currentLineTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        return _currentLineTcs.Task;
    }

    /// <summary>
    /// Starts a Say (async) and returns a Task that completes only when the typewriter effect finishes.
    /// This is the recommended way to do dialogue lines.
    /// </summary>
    public static async Task SayAsync(string speaker, string text, string? color = null, float msPerChar = 30f)
    {
        SayInternal(speaker, text, color, msPerChar);
        await WaitForCurrentLineAsync();
    }

    /// <summary>
    /// Immediately finishes the current line (shows full text).
    /// Useful for skipping.
    /// </summary>
    public static void CompleteCurrentLine()
    {
        if (!_visible || string.IsNullOrEmpty(_fullText)) return;

        _revealedChars = _fullText.Length;
        _isComplete = true;
        _timeSinceStart = float.MaxValue; // prevent any further revealing

        // Fill the builder with the entire text
        _displayBuilder.Clear();
        _displayBuilder.Append(_fullText);

        CompleteCurrentTcs(true);
    }

    /// <summary>
    /// Advances the typewriter effect.
    /// Uses time-since-start for stable target calculation + per-frame reveal limit
    /// to eliminate visible jerks even on unstable frame times.
    /// </summary>
    /// <summary>
    /// Advances the typewriter. 
    /// If no deltaTime is provided, it automatically uses <see cref="Engine.Time.SmoothDeltaTime"/>.
    /// </summary>
    public static void Update(double deltaTime = 0)
    {
        if (!_visible || _isComplete || string.IsNullOrEmpty(_fullText))
            return;

        // Use passed delta only as fallback. Prefer the centralized smooth time.
        float dt = deltaTime > 0 
            ? (float)deltaTime 
            : (Engine.Time.SmoothDeltaTime > 0 ? Engine.Time.SmoothDeltaTime : 0.016f);

        _timeSinceStart += dt;

        float secondsPerChar = _msPerChar / 1000f;

        // Calculate the ideal number of characters that should be visible by now
        float idealRevealed = _timeSinceStart / secondsPerChar;
        int targetRevealed = (int)idealRevealed;

        // Hard cap on characters revealed per frame.
        // Prevents ugly "bursts" when the game has a lag spike.
        const int MaxCharsPerFrame = 3;

        int charsToReveal = Math.Min(targetRevealed - _revealedChars, MaxCharsPerFrame);
        charsToReveal = Math.Max(0, charsToReveal);

        int charsRevealedThisFrame = 0;

        for (int i = 0; i < charsToReveal && _revealedChars < _fullText.Length; i++)
        {
            _displayBuilder.Append(_fullText[_revealedChars]);
            _revealedChars++;
            charsRevealedThisFrame++;
        }

        if (charsRevealedThisFrame > 0)
        {
            float currentCps = 1000f / _msPerChar;
        }

        if (_revealedChars >= _fullText.Length)
        {
            _isComplete = true;

            if (_displayBuilder.Length < _fullText.Length)
            {
                _displayBuilder.Clear();
                _displayBuilder.Append(_fullText);
            }

            CompleteCurrentTcs(true);
        }
    }

    /// <summary>
    /// Returns a snapshot suitable for pushing to JavaScript runtime bridges.
    /// The "text" field contains the already-revealed portion.
    /// </summary>
    public static DialogueSnapshot GetSnapshot()
    {
        return new DialogueSnapshot
        {
            Speaker = _speaker,
            Text = DisplayedText,
            FullText = _fullText,
            Color = _color,
            Visible = _visible,
            IsComplete = IsTypingComplete
        };
    }
}

/// <summary>
/// Data transfer object for the current dialogue state sent to JS.
/// </summary>
public readonly struct DialogueSnapshot
{
    public string Speaker { get; init; }
    public string Text { get; init; }           // already revealed part (for typewriter)
    public string FullText { get; init; }
    public string Color { get; init; }
    public bool Visible { get; init; }
    public bool IsComplete { get; init; }
}

/// <summary>
/// Aggregated runtime data snapshot that the Core engine pushes to all active UI elements each frame.
/// UI elements (especially HTML ones) receive this and decide how (or if) to forward it into their JS context.
/// </summary>
public readonly struct RuntimeSnapshot
{
    public double Fps { get; init; }
    public DialogueSnapshot Dialogue { get; init; }
    // Future: variables, time, player state, etc. can be added here.
}
