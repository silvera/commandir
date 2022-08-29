namespace Commandir.Core
{
    public class ActionHandlerRegistry
    {
        private readonly Dictionary<string, IActionHandler> _actions = new Dictionary<string, IActionHandler>(StringComparer.OrdinalIgnoreCase);
        
        public void RegisterActionHandler(IActionHandler action)
        {
             // TODO: Check for existing actions with the same name?
            _actions[action.Name] = action;
        }

        public IActionHandler? GetActionHandler(string name)
        {
            return _actions.TryGetValue(name, out IActionHandler? action) ? action : null;
        }
    }
}