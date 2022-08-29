using System.Reflection;

namespace Commandir.Core
{
    public class ActionRegistry
    {
        private readonly Dictionary<string, IAction> _actions = new Dictionary<string, IAction>(StringComparer.OrdinalIgnoreCase);
        
        public void RegisterActions()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach(Type type in assembly.GetExportedTypes())
            {
                if(type.GetInterface(nameof(IAction)) != null)
                {
                    RegisterAction(type);
                }
            }
        }

        public void RegisterAction(Type actionType)
        {
            IAction? action = Activator.CreateInstance(actionType) as IAction;
            if(action == null)
                throw new Exception($"Failed to create Action of type: {actionType}");

            RegisterAction(action);
        }

        public void RegisterAction(IAction action)
        {
             // TODO: Check for existing executors with the same name?
            _actions[action.Name] = action;
        }

        public IAction? GetAction(string name)
        {
            return _actions.TryGetValue(name, out IAction? action) ? action : null;
        }
    }
}