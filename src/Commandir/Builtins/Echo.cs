namespace Commandir.Builtins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Commandir.Core;

public class Echo : ICommand
{
    public Task ExecuteAsync(ICommandContext context)
    {
        if(!context.Parameters.TryGetValue("message", out object? messageObj))
            throw new Exception($"Failed to find parameter `message`.");
        
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Echo>();
        string? message = Convert.ToString(messageObj);
        logger.LogInformation($"{message}");
        return Task.CompletedTask;
    }
}