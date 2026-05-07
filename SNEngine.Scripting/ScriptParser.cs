using SNEngine.Scripting.Ast;
using SNEngine.Scripting.Parsing;

namespace SNEngine.Scripting
{
    /// <summary>
    /// Public facade for script parsing. API is 100% backward compatible.
    /// </summary>
    public static class ScriptParser
    {
        private static CommandParserFactory? _factory;
        private static ScriptLexer? _lexer;

        public static void Initialize(CommandParserFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _lexer = new ScriptLexer();
        }

        public static ScriptNode Parse(string source)
        {
            if (_factory == null || _lexer == null)
                throw new InvalidOperationException("Call ScriptParser.Initialize() first.");

            var tokens = _lexer.Tokenize(source);
            var core = new ScriptParserCore(_factory.CreateCommandParser());

            return core.Parse(tokens);
        }
    }
}