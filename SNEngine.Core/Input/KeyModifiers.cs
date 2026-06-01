using System;

namespace SNEngine.Core.Input;

/// <summary>
/// Keyboard modifier flags (platform-agnostic).
/// Used by the input system and key mappers.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None   = 0,
    Shift  = 1 << 0,
    Control= 1 << 1,
    Alt    = 1 << 2,
    // Windows: Windows key, macOS: Command key
    Meta   = 1 << 3
}
