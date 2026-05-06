using System;
using System.Threading;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Improved unique variable name generator.
/// Uses letters only (l_v_a, l_v_b, ..., l_v_aa, l_v_ab...) with random seed for safety.
/// </summary>
public static class UniqueVariableNameGenerator
{
    private static int _counter = 0;
    private static readonly Random _random = new Random();

    /// <summary>
    /// Generates short unique variable name using letters.
    /// Examples: l_v_a, l_v_b, l_v_c, ..., l_v_aa, l_v_ab, l_v_zz, etc.
    /// </summary>
    public static string Generate()
    {
        int id = Interlocked.Increment(ref _counter);

        // Base-26 conversion (a-z, aa-ab...)
        string letters = ToBase26(id);

        // Add small random suffix (2 chars) for extra safety between runs
        string randomSuffix = GenerateRandomSuffix(2);

        return $"l_v_{letters}{randomSuffix}";
    }

    /// <summary>
    /// Converts number to base-26 letter representation (1 → a, 26 → z, 27 → aa, etc.)
    /// </summary>
    private static string ToBase26(int number)
    {
        string result = "";
        while (number > 0)
        {
            number--; // make it 0-based
            result = (char)('a' + (number % 26)) + result;
            number /= 26;
        }
        return string.IsNullOrEmpty(result) ? "a" : result;
    }

    /// <summary>
    /// Generates short random alphanumeric suffix
    /// </summary>
    private static string GenerateRandomSuffix(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        char[] suffix = new char[length];

        for (int i = 0; i < length; i++)
        {
            suffix[i] = chars[_random.Next(chars.Length)];
        }

        return new string(suffix);
    }

    /// <summary>
    /// Reset counter before generating each scene
    /// </summary>
    public static void Reset()
    {
        _counter = 0;
    }
}