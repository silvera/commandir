using System.CommandLine.Invocation;

namespace Commandir.Core
{
    public interface ICommandContextHandler
    {
        Task HandleAsync(ICommandContext context);
    }
}
    