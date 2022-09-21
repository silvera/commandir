using Commandir;
using Commandir.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests
{
    public class CommandExecutorTests
    {
        private Parser CreateParser(string yaml, Action<Commandir.Core.CommandResult> commandResultHandler)
        {
            Commandir.Core.CommandResult? commandResult = null;

            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            CommandProvider commandProvider = new CommandProvider(loggerFactory);
            commandProvider.AddCommands(typeof(Commandir.Program).Assembly);
            CommandDefinition rootDefinition = new CommandDefinitionBuilder().AddYaml(yaml).Build();
            CommandLineCommand rootCommand = new CommandBuilder(loggerFactory, rootDefinition).Build();
            CommandExecutor commandExecutor = new CommandExecutor( loggerFactory, commandProvider, rootCommand, commandResultHandler);

            return new CommandLineBuilder(rootCommand)
                        .UseHost(host => 
                        {
                            host.ConfigureServices(services =>
                            {
                                services.AddSingleton<ITemplateFormatter, StubbleTemplateFormatter>();
                            });
                        })
                        .Build();
        }

        [Fact]
        public async Task Executes_Command()
        {   
            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Echo
                     parameters:
                        message: test
            ";

            Commandir.Core.CommandResult? commandResult = null;
            Parser parser = CreateParser(yaml, result => commandResult = result);
            await parser.InvokeAsync("test");

            Assert.NotNull(commandResult);
        }

        [Fact]
        public async Task Argument_Overrides_Parameter()
        {
            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Echo
                     parameters:
                        message: parameter
                     arguments:
                        - name: message
            ";

            Commandir.Core.CommandResult? commandResult = null;
            Parser parser = CreateParser(yaml, result => commandResult = result);
            await parser.InvokeAsync(new string[] { "test", "argument"});

            Assert.NotNull(commandResult);
            Assert.True(commandResult.Context.Parameters.TryGetValue("message", out object? messageObj));
            Assert.Equal("argument", Convert.ToString(messageObj));
        }

        [Fact]
        public async Task Option_Overrides_Argument()
        {
            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Echo
                     parameters:
                        message: parameter
                     arguments:
                        - name: message
                     options:
                        - name: message
                          required: true
            ";

            Commandir.Core.CommandResult? commandResult = null;
            Parser parser = CreateParser(yaml, result => commandResult = result);
            await parser.InvokeAsync(new string[] { "test", "argument", "--message", "option"});

            Assert.NotNull(commandResult);
            Assert.True(commandResult.Context.Parameters.TryGetValue("message", out object? messageObj));
            Assert.Equal("option", Convert.ToString(messageObj));
        }

        [Fact]
        public async Task Echo_Uses_Formatted_Value()
        {
            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Echo
                     parameters:
                        user: World
                        message: Hello {{user}}
            ";

            Commandir.Core.CommandResult? commandResult = null;
            Parser parser = CreateParser(yaml, result => commandResult = result);
            await parser.InvokeAsync(new string[] { "test" });

            Assert.NotNull(commandResult);
            Assert.Equal("Hello World", Convert.ToString(commandResult.ReturnValue));
        }

        [Fact]
        public async Task Shell_Uses_Formatted_Value()
        {
            string yaml = @"---
                commands:
                   - name: test
                     type: Commandir.Builtins.Shell
                     parameters:
                        user: World
                        command: echo Hello {{user}}
            ";

            Commandir.Core.CommandResult? commandResult = null;
            Parser parser = CreateParser(yaml, result => commandResult = result);
            await parser.InvokeAsync(new string[] { "test" });

            Assert.NotNull(commandResult);
            Assert.Equal("echo Hello World", Convert.ToString(commandResult.ReturnValue));
        }
    }    
}