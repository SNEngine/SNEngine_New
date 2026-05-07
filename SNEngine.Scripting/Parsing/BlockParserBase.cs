using SNEngine.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace SNEngine.Scripting.Parsing
{
    public abstract class BlockParserBase
    {
        protected StatementParser? _statementParser;

        public void Initialize(StatementParser statementParser)
        {
            _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
        }

        protected List<CommandNode> CollectBody(TokenReader reader, params string[] endKeywords)
        {
            var body = new List<CommandNode>();
            int safety = 0;

            while (!reader.Eof && reader.Current != null)
            {
                if (++safety > 10000) break;

                string currentLine = reader.PeekLineContent().Trim();
                var firstWord = currentLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                           .FirstOrDefault() ?? string.Empty;

                // Проверка по первому слову (точно как в StatementParser)
                bool isEnd = false;
                foreach (var kw in endKeywords)
                {
                    if (firstWord.Equals(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        isEnd = true;
                        break;
                    }
                }
                if (isEnd) break;

                var before = reader.Current;
                var stmt = _statementParser?.ParseNext(reader);

                if (stmt != null)
                {
                    body.Add(stmt);
                }
                else
                {
                    // Защита: если позиция не сдвинулась — принудительно потребляем строку
                    if (ReferenceEquals(before, reader.Current) && !reader.Eof)
                    {
                        reader.ConsumeCurrentLine();
                    }
                }
            }

            return body;
        }
    }
}