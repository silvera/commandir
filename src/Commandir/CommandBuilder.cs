using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace Commandir
{
    public abstract class CommandBuilder
    {
        public abstract Command Build(Func<IHost, Task> handler);
    }
}