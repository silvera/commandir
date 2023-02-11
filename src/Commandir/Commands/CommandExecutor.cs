using Serilog;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Commandir.Commands;

public class CommandValidationException : Exception
{
    public CommandValidationException(string message)
        : base(message)
    {
    }
}

public sealed class CommandExecutionResult
{
    /// <summary>
    /// The results of the execution of a command.
    /// May contain the results of multiple subcommands. 
    /// </summary>
    public IEnumerable<object?> Results { get; }
    public CommandExecutionResult(IEnumerable<object?> results)
    {
        Results = results;
    }
}



/// <summary>
/// Responsibe for executing the invoked command. 
/// </summary>
internal sealed class CommandExecutor
{
    private readonly ILogger _logger;
    private readonly ILogger _loggerFactory;
    
    public CommandExecutor(ILogger logger)
    {
        _loggerFactory = logger;
        _logger = logger.ForContext<CommandExecutor>();
    }

    private ICommandGroup GetCommandGroup(CommandWithData command, ParameterContext parameterContext)
    {
        string name = command.Name;
        bool? parallel = parameterContext.GetBooleanValue("parallel");
        bool executeCommandsInParallel = parallel ?? false;
        return executeCommandsInParallel 
            ? new ParallelCommandGroup(name, _loggerFactory) 
            : new SequentialCommandGroup(name, _loggerFactory);
    
    }
    private void AddCommands(InvocationContext invocationContext, CommandWithData command, ICommandGroup group, bool isRootGroup)
    {
        var parameterContext = new ParameterContext(invocationContext, command);

        if(command.Subcommands.Count == 0)
        {  
            var executable = new Executable(command, parameterContext, invocationContext.GetCancellationToken(), _loggerFactory);
            group.Add(executable);
        }
        else
        {
            ICommandGroup? subGroup = null;
            if(isRootGroup)
            {
                subGroup = group;
            }
            else
            {
                subGroup = GetCommandGroup(command, parameterContext);
                group.Add(subGroup);
            }
            
            foreach(CommandWithData subCommand in command.Subcommands)
            {
                AddCommands(invocationContext, subCommand, subGroup, false);
            }
        }
    }

    public async Task<CommandExecutionResult> ExecuteAsync(InvocationContext invocationContext)
    {
        // Manually surface any parse errors that should prevent command execution.
        // This is required because we're using Middlware to bypass the standard command execution pipeline.
        // This lets us handle invocations for internal commands that would normally result in a "Required command was not provided." error.
        ValidateCommandInvocation(invocationContext);

        CommandWithData? command = invocationContext.ParseResult.CommandResult.Command as CommandWithData;
        if(command == null)
            throw new Exception($"Failed to convert command to CommandWithData");

        var parameterContext = new ParameterContext(invocationContext, command);
        
        if(command.Subcommands.Any())
        {   
            // Restrict executable parameter check to internal (non-leaf) commands.
            // Default to false to maintain compatibilty with most other CLI libraries.
            bool? executable = parameterContext.GetBooleanValue("executable");
            bool isExecutable = executable ?? false; 
            if(!isExecutable)
            {
                throw new CommandValidationException($"Command `{command.Name}` is not executable.");
            }
        }
       
        _logger.Debug("Executing command `{CommandPath}`", command.GetPath());

        ICommandGroup commandGroup = GetCommandGroup(command, parameterContext);
        AddCommands(invocationContext, command, commandGroup, isRootGroup: true);

        if(commandGroup.Commands.Count == 0 && commandGroup.Groups.Count == 0)
            throw new CommandValidationException("No executable commands were found.");
       
        List<object?> results = await commandGroup.ExecuteAsync(); 
        return new CommandExecutionResult(results);
    }

    private static void ValidateCommandInvocation(InvocationContext invocationContext)
    {
        IReadOnlyList<ParseError> parseErrors = invocationContext.ParseResult.Errors; 
        
        // Check for unexpected parse errors
        if(parseErrors.Count > 0)
        {
            if(parseErrors.Count > 1)
            {
                // There is more than one parse error; return the last error.
                throw new CommandValidationException(parseErrors.Last().Message);
            }
            else
            {
                ParseError error = parseErrors.First();
             
                // If no commands were supplied, return the error.
                if(invocationContext.ParseResult.Tokens.Count == 0)
                    throw new CommandValidationException(error.Message);
             
                // Ensure the one parse error is the expected error.
                if(error.Message != "Required command was not provided.")
                    throw new CommandValidationException(error.Message);
            }
        }
    }
}