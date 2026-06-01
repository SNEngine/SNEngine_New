namespace SNEngine.Core.Engine;

/// <summary>
/// Minimal shared dialogue state for pushing "Say" lines into all active Ultralight views.
/// Lives in Core so both SNEngine.API (writer) and SNEngine.UI (reader/pusher to JS) can access it
/// without creating circular references.
/// </summary>
public static class DialogueState
{
    public static string Speaker { get; private set; } = string.Empty;
    public static string Text { get; private set; } = string.Empty;
    public static string Color { get; private set; } = "#FFFFFF";
    public static bool Visible { get; private set; } = false;

    public static void Set(string speaker, string text, string color, bool visible)
    {
        Speaker = speaker ?? string.Empty;
        Text = text ?? string.Empty;
        Color = string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color;
        Visible = visible;
    }

    public static void Clear()
    {
        Visible = false;
        Speaker = string.Empty;
        Text = string.Empty;
    }

    public static (string Speaker, string Text, string Color, bool Visible) GetCurrent()
        => (Speaker, Text, Color, Visible);
}
