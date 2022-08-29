using Commandir.Core;

namespace Commandir.Actions
{
    public class ConsoleAction : IActionHandler
    {
        private readonly Func<ActionExecutionContext, string> _formatFn;
        public ConsoleAction(Func<ActionExecutionContext, string> formatFn)
        {
            _formatFn = formatFn;
        }

        public string Name => "console";
        public Task ExecuteAsync(ActionExecutionContext context)
        {
            Console.WriteLine(_formatFn(context));
            return Task.CompletedTask;
        }
    }
}