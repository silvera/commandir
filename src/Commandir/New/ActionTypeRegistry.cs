namespace Commandir.New
{
    public class ActionTypeRegistry
    {
        private readonly Dictionary<string, Type> _typeNames = new Dictionary<string, Type>();

        public void RegisterType(string actionTypeName, Type actionType)
        {
            _typeNames[actionTypeName] = actionType;
        }

        public Type? GetType(string actionTypeName)
        {
            return _typeNames.TryGetValue(actionTypeName, out Type? actionType) ? actionType : default;
        }
    }
}