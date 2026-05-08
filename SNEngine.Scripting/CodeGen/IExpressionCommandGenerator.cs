using Microsoft.CodeAnalysis.CSharp.Syntax;
using SNEngine.Scripting.Ast;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Интерфейс для команд, которые можно использовать в правой части присваивания
/// (Get String from, Get Int from и т.д.)
/// </summary>
public interface IExpressionCommandGenerator : ICommandCodeGenerator
{
    /// <summary>
    /// Генерирует выражение (не statement) для использования в правой части =
    /// </summary>
    ExpressionSyntax GenerateExpression(string innerExpression);
}