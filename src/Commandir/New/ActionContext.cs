using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Commandir.New
{
    public interface IActionContextProvider
    {
        IReadOnlyList<ActionData> GetActions();
        IReadOnlyDictionary<string, object?> GetParameters();
        CancellationToken GetCancellationToken();
    }

    public class InvocationContextActionContextProvider : IActionContextProvider
    {
        private readonly InvocationContext _invocationContext;
        public InvocationContextActionContextProvider(InvocationContext invocationContext)
        {
            _invocationContext = invocationContext; 
        }

        public CancellationToken GetCancellationToken() => _invocationContext.GetCancellationToken();

        public IReadOnlyList<ActionData> GetActions()
        {
            ActionCommand? command = _invocationContext.ParseResult.CommandResult.Command as ActionCommand;
            if(command == null)
                throw new Exception();

            return command.Actions;
        }

        public IReadOnlyDictionary<string, object?> GetParameters()
        {
            Dictionary<string, object?> parameters = new Dictionary<string, object?>();
            
            ParseResult result = _invocationContext.BindingContext.ParseResult;
            ActionCommand? command = result.CommandResult.Command as ActionCommand;
            if(command == null)
                throw new Exception();

            foreach(New.ActionData action in command.Actions)
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
    
    // public class ActionContext
    // {
    //     private readonly Dictionary<string, object?> _parameters = new Dictionary<string, object?>();
    //     public void AddParameter(string name, object? value) => _parameters[name] = value;
    //     public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    //     private readonly IActionContextProvider _parameterProvider;
    //     public ActionContext(IActionContextProvider parameterProvider)
    //     {
    //         _parameterProvider = parameterProvider;
    //         foreach(var pair in _parameterProvider.GetParameters())
    //         {
    //             AddParameter(pair.Key, pair.Value);
    //         }
    //     }
    // }
}