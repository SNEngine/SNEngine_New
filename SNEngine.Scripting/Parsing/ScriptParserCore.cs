using Pidgin;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public sealed class ScriptParserCore
    {
        private readonly StatementParser _statementParser;
        private readonly FunctionParser _functionParser;
        private readonly ForBlockParser _forBlockParser;

        public ScriptParserCore(Parser<char, CommandNode> commandParser)
        {
            if (commandParser == null)
                throw new ArgumentNullException(nameof(commandParser));

            var ifBlockParser = new IfBlockParser();
            var forBlockParser = new ForBlockParser();
            var statementParser = new StatementParser(commandParser, ifBlockParser, forBlockParser);

            ifBlockParser.Initialize(statementParser);
            forBlockParser.Initialize(statementParser);

            _statementParser = statementParser;
            _forBlockParser = forBlockParser;
            _functionParser = new FunctionParser(statementParser);
        }

        public ScriptNode Parse(IReadOnlyList<ScriptToken> tokens)
        {
            var reader = new TokenReader(tokens);
            string? sceneName = null;
            var commands = new List<CommandNode>();
            var functions = new List<FunctionNode>();

            while (!reader.Eof && reader.Current != null)
            {
                string lineContent = reader.PeekLineContent();

                if (lineContent.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
                {
                    sceneName = reader.ConsumeFullCommandLine()
                                      .Replace("name:", "", StringComparison.OrdinalIgnoreCase)
                                      .Trim();
                    continue;
                }

                if (lineContent.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
                {
                    functions.Add(_functionParser.Parse(reader));
                    continue;
                }

                var statement = _statementParser.ParseNext(reader);
                if (statement != null) commands.Add(statement);
            }

            return new ScriptNode(sceneName, commands, functions);
        }
    }
}