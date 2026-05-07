using Pidgin;
using SNEngine.Scripting.Ast;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Lightweight orchestrator that delegates parsing to specialized parsers.
    /// </summary>
    public sealed class ScriptParserCore
    {
        private readonly Parser<char, CommandNode> _commandParser;
        private readonly FunctionParser _functionParser;
        private readonly IfBlockParser _ifBlockParser;

        public ScriptParserCore(Parser<char, CommandNode> commandParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
            _functionParser = new FunctionParser(commandParser);
            _ifBlockParser = new IfBlockParser(commandParser);
        }

        public ScriptNode Parse(IReadOnlyList<ScriptToken> tokens)
        {
            var reader = new TokenReader(tokens);
            string? sceneName = null;
            var commands = new List<CommandNode>();
            var functions = new List<FunctionNode>();

            while (!reader.Eof)
            {
                if (reader.Match("name:"))
                {
                    sceneName = reader.ConsumeValueAfter("name:");
                    continue;
                }

                if (reader.Match("function "))
                {
                    functions.Add(_functionParser.Parse(reader));
                    continue;
                }

                if (reader.Match("if ") && reader.Current.Value.Contains("then", StringComparison.OrdinalIgnoreCase))
                {
                    var ifNode = _ifBlockParser.Parse(reader);
                    if (ifNode != null)
                        commands.Add(ifNode);
                    continue;
                }

                // Regular command
                var result = _commandParser.Parse(reader.Current.Value);
                if (result.Success)
                    commands.Add(result.Value);

                reader.Consume();
            }

            return new ScriptNode(sceneName, commands, functions);
        }
    }
}