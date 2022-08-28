namespace Commandir.Core
{
    public class CommandHandler : ICommandContextHandler
    {
        // TODO: Implement dynamic lookup of handlers based on context.Handler

        public Task HandleAsync(ICommandContext context)
        {
            ArgumentContext? greetingContext = context.Arguments.FirstOrDefault(i => i.Name == "greeting"); 
            OptionContext? nameContext = context.Options.FirstOrDefault(i => i.Name == "name");
            Console.WriteLine($"{greetingContext?.Value} {nameContext?.Value}");
            return Task.CompletedTask;
        }
    }
}
    