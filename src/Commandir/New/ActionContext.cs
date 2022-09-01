using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Commandir.New
{
    public interface IActionContext
    {
        IReadOnlyDictionary<string, object?> Parameters { get; }
        void AddParameter(string name, object? value);
    }

    public class ActionContext : IActionContext
    {
        private readonly Dictionary<string, object?> _parameters = new Dictionary<string, object?>();
        public void AddParameter(string name, object? value) => _parameters[name] = value;
        public IReadOnlyDictionary<string, object?> Parameters => _parameters;

        public ActionContext(InvocationContext invocationContext)
        {
            ParseResult result = invocationContext.BindingContext.ParseResult;
            ActionCommand? command = result.CommandResult.Command as ActionCommand;
            if(command == null)
                throw new Exception();
                
            foreach(New.ActionData action in command.Actions)
            {
                foreach(var pair in action)
                {
                    AddParameter(pair.Key, pair.Value);
                }
            }

            foreach(Argument argument in command.Arguments)
            {
                object? value = result.GetValueForArgument(argument);
                AddParameter(argument.Name, value);
            }

            foreach(Option option in command.Options)
            {
                object? value = result.GetValueForOption(option);
                AddParameter(option.Name, value);
            }
        }
    }
}