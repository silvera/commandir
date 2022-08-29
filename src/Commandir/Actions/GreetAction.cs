using Commandir.Core;

namespace Commandir.Actions
{
    public class GreetAction : IAction
    {
        public string Name => "greet";
        public Task ExecuteAsync(ActionExecutionContext context)
        {
            ParameterExecutionContext? greetingContext = context.Parameters.FirstOrDefault(i => i.Name == "greeting"); 
            ParameterExecutionContext? nameContext = context.Parameters.FirstOrDefault(i => i.Name == "name");
            Console.WriteLine($"{greetingContext?.Value} {nameContext?.Value}");
            return Task.CompletedTask;
        }
    }
}