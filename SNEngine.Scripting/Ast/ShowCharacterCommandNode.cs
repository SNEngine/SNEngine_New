using Pidgin;
using SNEngine.Scripting.Ast;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace SNEngine.Scripting.Ast;

/// <summary>
/// Show Character Nagatoro angry
/// </summary>
[SnCommand("Show Character")]
public sealed class ShowCharacterCommandNode : CommandNode, IParsableCommand
{
    public string CharacterName { get; }
    public string Emotion { get; } = "happy";

    public ShowCharacterCommandNode(string characterName, string emotion = "happy")
    {
        CharacterName = characterName;
        Emotion = emotion;
    }

    public static Parser<char, CommandNode> Parser { get; } =
        Try(String("Show")
            .Before(SkipWhitespaces)
            .Then(String("Character"))
            .Before(SkipWhitespaces)
            .Then(CommonParsers.Identifier)
            .Then(Whitespace.Then(CommonParsers.Identifier).Optional(), (name, emotionOpt) =>
                new ShowCharacterCommandNode(name, emotionOpt.HasValue ? emotionOpt.Value : "happy"))
            .Cast<CommandNode>());
}