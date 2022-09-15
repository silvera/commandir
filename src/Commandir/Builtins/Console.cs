namespace Commandir.Builtins;

using System.Threading.Tasks;
using Commandir.Core;

public class Console : ICommand
{
    public Task ExecuteAsync(ICommandContext context)
    {
        if(!context.Parameters.TryGetValue("message", out object? messageObj))
            throw new Exception($"Failed to find parameter `message`.");
        
        string? message = Convert.ToString(messageObj);

        string? prefix = string.Empty;
        if(context.Parameters.TryGetValue("prefix", out object? prefixObj))
            prefix = Convert.ToString(prefixObj);

        System.Console.WriteLine($"{prefix} {message}");
        return Task.CompletedTask;
    }
}