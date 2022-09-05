using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Commandir
{
    public interface IActionContextProvider
    {
        IReadOnlyList<Action> GetActions();
        IReadOnlyDictionary<string, object?> GetParameters();
        CancellationToken GetCancellationToken();
    }

    public class InvocationContextActionContextProvider : IActionContextProvider
    {
        private readonly InvocationContext _invocationContext;
        private readonly IServiceProvider _serviceProvider;
        public InvocationContextActionContextProvider(InvocationContext invocationContext, IServiceProvider serviceProvider)
        {
            _invocationContext = invocationContext;
            _serviceProvider = serviceProvider; 
        }

        public CancellationToken GetCancellationToken() => _invocationContext.GetCancellationToken();

        public IReadOnlyList<Action> GetActions()
        {
            ActionCommand? command = _invocationContext.ParseResult.CommandResult.Command as ActionCommand;
            if(command == null)
                throw new Exception();

            List<Type> actionTypes = new List<Type>();
            foreach(ActionData actionData in command.Actions)
            {
                // Need the full type name (w/ namespace):
                string actionTypeName = "Commandir." + actionData.Name;
                Type? actionType = Type.GetType(actionTypeName);
                if(actionType == null)
                    throw new Exception();

                actionTypes.Add(actionType);
            }
            
            List<Action> actions = new List<Action>();
            foreach(Type actionType in actionTypes)
            {
                Action? action = _serviceProvider.GetRequiredService(actionType) as Action;
                if(action == null)
                    throw new Exception($"Failed to find Action for type `{actionType}`");

                actions.Add(action);
            }

            return actions;
        }

        public IReadOnlyDictionary<string, object?> GetParameters()
        {
            Dictionary<string, object?> parameters = new Dictionary<string, object?>();
            
            ParseResult result = _invocationContext.BindingContext.ParseResult;
            ActionCommand? command = result.CommandResult.Command as ActionCommand;
            if(command == null)
                throw new Exception();

            foreach(ActionData action in command.Actions)
            {
                foreach(var pair in action)
                {
                    parameters[pair.Key]= pair.Value;
                }
            }

            foreach(Argument argument in command.Arguments)
            {
                object? value = result.GetValueForArgument(argument);
                parameters[argument.Name] = value;
            }

            foreach(Option option in command.Options)
            {
                object? value = result.GetValueForOption(option);
                parameters[option.Name] = value;
            }
            
            return parameters;
        }
    }
}