using System.Collections.Generic;

namespace SNEngine.Scripting.Ast
{
    public sealed class ForCommandNode : CommandNode
    {
        public string Variable { get; }
        public string Init { get; }
        public string Condition { get; }
        public string Increment { get; }
        public List<CommandNode> Body { get; }

        public ForCommandNode(string variable, string init, string condition, string increment, List<CommandNode> body)
        {
            Variable = variable;
            Init = init;
            Condition = condition;
            Increment = increment;
            Body = body;
        }
    }
}