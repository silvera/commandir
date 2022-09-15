using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests
{
    public class CommandExecutorTests
    {
        [Fact]
        public async Task Executes_Command()
        {   
            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Console
                     parameters:
                        message: Test Message
            ";

            Core.CommandData rootData = new YamlCommandDataBuilder(yaml).Build();
            CommandLineCommand rootCommand = new CommandBuilder(rootData, CommandExecutor.ExecuteAsync).Build();

            var parser = new CommandLineBuilder(rootCommand)
                        .UseHost()
                        .Build();

            await parser.InvokeAsync("test");
        }
    }    
}