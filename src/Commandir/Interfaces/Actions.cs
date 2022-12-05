using Microsoft.Extensions.Logging;

namespace Commandir.Interfaces;


public sealed class ActionRequest
{
    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken { get; }

    public ActionRequest(IServiceProvider services, CancellationToken cancellationToken)
    {
        Services = services;
        CancellationToken = cancellationToken;
    }
}

public sealed class ActionContext
{
    public IServiceProvider Services { get; private set;}
    public CancellationToken CancellationToken { get; private set; }

    public ActionContext(IServiceProvider services, CancellationToken cancellationToken)
    {
        Services = services;
        CancellationToken = cancellationToken;
    }
}

public sealed class ActionResponse
{
    public object? Value { get; set; }
}

public sealed class ActionResult
{
    public object? Value { get; set; }
}

public interface IActionHandler
{
    Task<ActionResponse> HandleAsync(ActionRequest request);
}

public interface IActionHandlerProvider
{
    IActionHandler? GetAction(string actionName);
}

public interface IActionExecutor
{
    Task<ActionResponse> ExecuteAsync(ActionExecutionData data);
}

public sealed class ActionExecutionData
{
    public IServiceProvider Services { get; set; }
    public string Action { get; set; }
    public Dictionary<string, object?> Parameters { get; set; }
    public CancellationToken CancellationToken { get; set; } 
}


// public class CommandInvocationContext
// {
//     public string Action { get; private set; }
//     public Dictionary<string, object?> Parameters { get; private set; }
//     public CancellationToken CancellationToken { get; private set; } 

//     public static CommandInvocationContext Create(IServiceProvider services)
//     {
//         var invocationContext = services.GetRequiredService<InvocationContext>();
//         var parseResult = invocationContext.ParseResult;
//         var command = (CommandirCommand)parseResult.CommandResult.Command;
        
//         Dictionary<string, object?> parameters = new Dictionary<string, object?>();
//         foreach(var pair in command.Parameters)
//         {
//             if(pair.Value != null)
//                 parameters[pair.Key] = pair.Value;
//         }

//         foreach(Argument argument in command.Arguments)
//         {
//             object? value = parseResult.GetValueForArgument(argument);
//             if(value != null)
//             {
//                 parameters[argument.Name] =  value;
//             }
//         }

//         foreach(Option option in command.Options)
//         {
//             object? value = parseResult.GetValueForOption(option);
//             if(value != null)
//             {
//                 parameters[option.Name] = value;
//             } 
//         }

//         return new CommandInvocationContext
//         {
//             Action = command.Action!,
//             Parameters = parameters,
//             CancellationToken = invocationContext.GetCancellationToken()
//         };
//     }
// }