namespace SNEngine.Core.Input;

/// <summary>
/// Engine-level key codes (abstracted from Silk.NET).
/// Extend as needed.
/// </summary>
public enum Key
{
    Unknown = 0,

    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Special
    Escape, Enter, Space, Tab, Backspace, Delete,
    LeftShift, RightShift, LeftControl, RightControl,
    LeftAlt, RightAlt,

    // Arrows
    Left, Right, Up, Down,

    // Function
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
}
