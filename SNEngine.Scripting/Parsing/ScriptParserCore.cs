using Pidgin;
using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Lightweight orchestrator with safe two-phase initialization.
    /// </summary>
    public sealed class ScriptParserCore
    {
        private readonly StatementParser _statementParser;
        private readonly FunctionParser _functionParser;

        public ScriptParserCore(Parser<char, CommandNode> commandParser)
        {
            if (commandParser == null)
                throw new ArgumentNullException(nameof(commandParser));

            var ifBlockParser = new IfBlockParser(commandParser);
            _statementParser = new StatementParser(commandParser, ifBlockParser);
            ifBlockParser.Initialize(_statementParser);
            ScriptLexer.AutoRegisterAllCommands();

            _functionParser = new FunctionParser(_statementParser);
        }

        public ScriptNode Parse(IReadOnlyList<ScriptToken> tokens)
        {
            var reader = new TokenReader(tokens);
            string? sceneName = null;
            var commands = new List<CommandNode>();
            var functions = new List<FunctionNode>();

            while (!reader.Eof && reader.Current != null)
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

                var statement = _statementParser.ParseNext(reader);
                if (statement != null)
                    commands.Add(statement);
            }

            return new ScriptNode(sceneName, commands, functions);
        }
    }
}