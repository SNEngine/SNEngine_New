using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting;

/// <summary>
/// Общие парсеры для .sn
/// </summary>
public static class CommonParsers
{
    public static readonly Parser<char, string> Identifier =
        LetterOrDigit.Or(Char('_')).AtLeastOnceString();

    public static readonly Parser<char, string> StringLiteral =
        Char('"')
            .Then(Any.Until(Char('"')))
            .Select(chars => new string(chars.ToArray()))
            .Before(Char('"'));
}