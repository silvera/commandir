using System.CommandLine;
using System.CommandLine.Invocation;

namespace Commandir.Core
{
    public class CommandWithHandler : Command
    {
        public CommandWithHandler(string name, string description, HandlerContext handlerContext)
            : base(name, description)
        {
            Handler = new AnonymousCommandHandler(invocationContext => 
                {
                    // Create CommandContext object here?
                    List<ArgumentContext> arguments = new List<ArgumentContext>();
                    foreach(Argument argument in Arguments)
                    {
                        object? value = invocationContext.ParseResult.GetValueForArgument(argument);
                        ArgumentContext argumentContext = new ArgumentContext(argument.Name, value);
                        arguments.Add(argumentContext);
                    }

                    List<OptionContext> options = new List<OptionContext>();
                    foreach(Option option in Options)
                    {
                        object? value = invocationContext.ParseResult.GetValueForOption(option);
                        OptionContext optionContext = new OptionContext(option.Name, value);
                        options.Add(optionContext);
                    }

                    CommandContext commandContext = new CommandContext(arguments, options);
                    return handlerContext.Handler.HandleAsync(commandContext);
                });
        }
    }
}