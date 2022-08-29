using System.CommandLine;
using Commandir.Core;

namespace Commandir
{
    public class CommandirCommand : Command
    {
        private readonly List<ActionContext> _actions = new List<ActionContext>();
        public IReadOnlyList<ActionContext> Actions => _actions;
        public void AddAction(ActionContext action) => _actions.Add(action);

        public CommandirCommand(string name, string description)
            : base(name, description)
        {
        }
    }

    public class CommandirRootCommand : CommandirCommand
    {
        public CommandirRootCommand(string description)
            : base("root", description)
        {
        }
    }
}