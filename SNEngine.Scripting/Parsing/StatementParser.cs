using Pidgin;
using SNEngine.Scripting.Ast;
using System;

namespace SNEngine.Scripting.Parsing
{
    /// <summary>
    /// Single point of truth for deciding what kind of statement we are looking at.
    /// This is where all high-level constructs (if, while, for, etc.) will be registered in the future.
    /// Keeps FunctionParser and ScriptParserCore clean and extensible.
    /// </summary>
    public sealed class StatementParser
    {
        private readonly Parser<char, CommandNode> _commandParser;
        private readonly IfBlockParser _ifBlockParser;

        public StatementParser(Parser<char, CommandNode> commandParser, IfBlockParser ifBlockParser)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
            _ifBlockParser = ifBlockParser ?? throw new ArgumentNullException(nameof(ifBlockParser));
        }

        /// <summary>
        /// Main entry point — tries to parse the next statement at current position.
        /// </summary>
        public CommandNode? ParseNext(TokenReader reader)
        {
            if (reader.Eof) return null;

            int safety = 0;
            while (!reader.Eof && safety++ < 1000)
            {
                if (reader.Match("if ") && reader.Current.Value.Contains("then", StringComparison.OrdinalIgnoreCase))
                {
                    return _ifBlockParser.Parse(reader);
                }

                var result = _commandParser.Parse(reader.Current.Value);
                if (result.Success)
                {
                    reader.Consume();
                    return result.Value;
                }

                reader.Consume(); // ← обязательно потребляем токен, даже если не распознали
                return null;
            }

            Console.WriteLine("[Warning] StatementParser safety limit reached");
            return null;
        }
    }

}