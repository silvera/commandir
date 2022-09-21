namespace Commandir.Builtins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Commandir.Core;

public class Echo : ICommand
{
    public Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        if(!context.Parameters.TryGetValue("message", out object? messageObj))
            throw new Exception($"Failed to find parameter `message`.");
        
        string? message = Convert.ToString(messageObj);
        if(message == null)
            throw new Exception("Parameter `message` was null.");

        var templateFormatter = context.Services.GetRequiredService<ITemplateFormatter>();
        if(templateFormatter == null)
            throw new Exception("Failed to get TemplateFormatter.");

        string formattedMessage = templateFormatter.Format(message, context.Parameters);

        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Echo>();
        
        logger.LogInformation($"{formattedMessage}");
        return Task.FromResult(new CommandResult(context, 0, formattedMessage));
    }
}