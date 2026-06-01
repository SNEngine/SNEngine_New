using SNEngine.Core.Input;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Converts platform-agnostic key data from <see cref="SNEngine.Core.Input.KeyMapper"/>
/// into UltralightNet-specific structures.
/// </summary>
public static class UltralightKeyMapper
{
    /// <summary>
    /// Converts our engine key into a virtual key code for Ultralight.
    /// </summary>
    public static int ToVirtualKey(Key key)
    {
        return SNEngine.Core.Input.KeyMapper.ToVirtualKey(key);
    }

    /// <summary>
    /// Converts our <see cref="KeyModifiers"/> into a raw uint suitable for ULKeyEvent (avoids enum name differences across UltralightNet versions).
    /// </summary>
    public static uint ToUltralightModifiersRaw(KeyModifiers modifiers)
    {
        uint result = 0;

        if ((modifiers & KeyModifiers.Shift) != 0)   result |= (1u << 0);
        if ((modifiers & KeyModifiers.Control) != 0) result |= (1u << 1);
        if ((modifiers & KeyModifiers.Alt) != 0)     result |= (1u << 2);
        if ((modifiers & KeyModifiers.Meta) != 0)    result |= (1u << 3);

        return result;
    }
}
