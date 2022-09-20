using Commandir.Core;
using Microsoft.Extensions.Logging;
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
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Echo
                     parameters:
                        message: Test Message
            ";

            CommandProvider commandProvider = new CommandProvider(loggerFactory);
            commandProvider.AddCommands(typeof(Commandir.Program).Assembly);
            CommandExecutor commandExecutor = new CommandExecutor(commandProvider, loggerFactory);
            CommandDefinition rootDefinition = new CommandDefinitionBuilder().AddYaml(yaml).Build();
            CommandLineCommand rootCommand = new CommandBuilder(rootDefinition, commandExecutor.ExecuteAsync, loggerFactory).Build();

            var parser = new CommandLineBuilder(rootCommand)
                        .UseHost()
                        .Build();

            await parser.InvokeAsync("test");
        }
    }    
}