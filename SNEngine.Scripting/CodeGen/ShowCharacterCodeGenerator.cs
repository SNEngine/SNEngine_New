using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

[SnCodeGenerator(typeof(ShowCharacterCommandNode))]
public sealed class ShowCharacterCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not ShowCharacterCommandNode ch)
            return SyntaxFactory.ParseStatement("// Invalid ShowCharacterCommandNode");

        return SyntaxFactory.ParseStatement($"CharacterAPI.Show(\"{ch.CharacterName}\", \"{ch.Emotion}\");");
    }
}