using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;
using SNEngine.Scripting.AST;
using SNEngine.Scripting.CodeGen;

namespace SNEngine.Scripting.Generators;

/// <summary>
/// Generates raw C# code from NativeCommandNode.
/// Uses a proper Block with preserved formatting.
/// </summary>
[SnCodeGenerator(typeof(NativeCommandNode))]
public sealed class NativeCodeGenerator : ICommandCodeGenerator
{
    public StatementSyntax Generate(CommandNode node)
    {
        if (node is not NativeCommandNode native)
            throw new InvalidOperationException("Invalid node for NativeCodeGenerator");

        string rawCode = native.RawCSharpCode.Trim();

        // Оборачиваем в явный блок и добавляем комментарий для отладки
        string blockCode = $@"
{{
    {rawCode}
}}";

        // Парсим как Block — это самый надёжный способ сохранить форматирование
        var syntax = SyntaxFactory.ParseStatement(blockCode);

        return syntax
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            .NormalizeWhitespace();   // ← это важно
    }
}