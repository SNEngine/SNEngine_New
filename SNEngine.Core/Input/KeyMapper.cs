using System;
using System.Collections.Generic;

namespace SNEngine.Core.Input;

public static class KeyMapper
{
    private static readonly Dictionary<Silk.NET.Input.Key, Key> SilkToEngineMap = InitializeSilkMapping();

    private static Dictionary<Silk.NET.Input.Key, Key> InitializeSilkMapping()
    {
        var map = new Dictionary<Silk.NET.Input.Key, Key>();
        var silkKeys = Enum.GetValues<Silk.NET.Input.Key>();

        foreach (var silkKey in silkKeys)
        {
            string silkKeyName = silkKey.ToString();

            if (Enum.TryParse<Key>(silkKeyName, out var matchedKey))
            {
                map[silkKey] = matchedKey;
                continue;
            }

            var fallbackKey = silkKeyName switch
            {
                "ShiftLeft" => Key.LeftShift,
                "ShiftRight" => Key.RightShift,
                "ControlLeft" => Key.LeftControl,
                "ControlRight" => Key.RightControl,
                "AltLeft" => Key.LeftAlt,
                "AltRight" => Key.RightAlt,
                "Number0" => Key.D0,
                "Number1" => Key.D1,
                "Number2" => Key.D2,
                "Number3" => Key.D3,
                "Number4" => Key.D4,
                "Number5" => Key.D5,
                "Number6" => Key.D6,
                "Number7" => Key.D7,
                "Number8" => Key.D8,
                "Number9" => Key.D9,
                _ => Key.Unknown
            };

            map[silkKey] = fallbackKey;
        }

        return map;
    }

    public static Key FromSilkKey(Silk.NET.Input.Key key)
    {
        return SilkToEngineMap.TryGetValue(key, out var targetKey) ? targetKey : Key.Unknown;
    }

    public static int ToVirtualKey(Key key)
    {
        return key switch
        {
            Key.A => 0x41,
            Key.B => 0x42,
            Key.C => 0x43,
            Key.D => 0x44,
            Key.E => 0x45,
            Key.F => 0x46,
            Key.G => 0x47,
            Key.H => 0x48,
            Key.I => 0x49,
            Key.J => 0x4A,
            Key.K => 0x4B,
            Key.L => 0x4C,
            Key.M => 0x4D,
            Key.N => 0x4E,
            Key.O => 0x4F,
            Key.P => 0x50,
            Key.Q => 0x51,
            Key.R => 0x52,
            Key.S => 0x53,
            Key.T => 0x54,
            Key.U => 0x55,
            Key.V => 0x56,
            Key.W => 0x57,
            Key.X => 0x58,
            Key.Y => 0x59,
            Key.Z => 0x5A,

            Key.D0 => 0x30,
            Key.D1 => 0x31,
            Key.D2 => 0x32,
            Key.D3 => 0x33,
            Key.D4 => 0x34,
            Key.D5 => 0x35,
            Key.D6 => 0x36,
            Key.D7 => 0x37,
            Key.D8 => 0x38,
            Key.D9 => 0x39,

            Key.Escape => 0x1B,
            Key.Enter => 0x0D,
            Key.Space => 0x20,
            Key.Tab => 0x09,
            Key.Backspace => 0x08,

            Key.Left => 0x25,
            Key.Right => 0x27,
            Key.Up => 0x26,
            Key.Down => 0x28,

            Key.LeftShift => 0xA0,
            Key.RightShift => 0xA1,
            Key.LeftControl => 0xA2,
            Key.RightControl => 0xA3,
            Key.LeftAlt => 0xA4,
            Key.RightAlt => 0xA5,

            _ => 0
        };
    }

    public static KeyModifiers GetCurrentModifiers()
    {
        KeyModifiers mods = KeyModifiers.None;

        if (Input.GetKey(Key.LeftShift) || Input.GetKey(Key.RightShift))
            mods |= KeyModifiers.Shift;

        if (Input.GetKey(Key.LeftControl) || Input.GetKey(Key.RightControl))
            mods |= KeyModifiers.Control;

        if (Input.GetKey(Key.LeftAlt) || Input.GetKey(Key.RightAlt))
            mods |= KeyModifiers.Alt;

        return mods;
    }
}