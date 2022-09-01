using System.CommandLine;
using Commandir.New;

namespace Commandir
{
    // TODO: Move to Commandir namespace

    public class ActionCommand : Command
    {
        private readonly List<ActionData> _actions = new List<ActionData>();
        public void AddAction(ActionData action) => _actions.Add(action);
        public IReadOnlyList<ActionData> Actions => _actions;

        public ActionCommand(string name, string description)
            : base(name, description)
        {
        }
    }
}