using Commandir.Interfaces;

namespace Commandir.Executors;

public sealed class Test : IExecutor
{
    public async Task<object?> ExecuteAsync(IExecutionContext context)
    {
        object? messageObj = context.ParameterContext.GetParameterValue("message");
        if(messageObj is null)
            throw new Exception("Failed to find `message` parameter.");

        string? messageStr = messageObj as string;
        if(messageStr is null)
            throw new Exception("Failed to convert `message` parameter to string.");

        string formattedMessageStr = context.ParameterContext.FormatParameters(messageStr);

        object? delaySecondsObj = context.ParameterContext.GetParameterValue("delaySeconds");
        if(delaySecondsObj is not null)
        {
            int? delaySeconds = Convert.ToInt32(delaySecondsObj);
            if(delaySeconds is not null)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds.Value));
            }
        }

        return formattedMessageStr;
    }
}