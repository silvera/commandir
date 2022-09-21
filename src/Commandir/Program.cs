using Commandir.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

namespace Commandir
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Override("Commandir", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Logger = logger;
            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(logger);

            Commandir.Core.CommandResult? commandResult = null;

            try
            {
                await BuildCommandLine(loggerFactory, i => commandResult = i)
                .UseHost(host => {
                    host.UseSerilog(logger);
                    host.ConfigureServices(services =>
                    {
                        services.AddSingleton<ITemplateFormatter, StubbleTemplateFormatter>();
                    });
                })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);

                return commandResult?.ReturnCode ?? 1;
            }
            catch(Exception e)
            {
                loggerFactory
                    .CreateLogger<Program>()
                    .LogCritical("Error: {Error}", e.Message);
                
                return 1;
            }
        }

        private static CommandLineBuilder BuildCommandLine(ILoggerFactory loggerFactory, Action<Commandir.Core.CommandResult> commandResultHandler)
        {
            CommandDefinition? rootDefinition = new CommandDefinitionBuilder()
                                .AddYamlFile(Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml"))
                                .Build();
            
            CommandLineCommand rootCommand = new CommandBuilder(loggerFactory, rootDefinition)
                                .Build();
            
            CommandProvider commandProvider = new CommandProvider(loggerFactory);
            commandProvider.AddCommands(typeof(Program).Assembly);
            
            CommandExecutor commandExecutor = new CommandExecutor(loggerFactory, commandProvider, rootCommand, commandResultHandler);
            return new CommandLineBuilder(rootCommand);
        }
    }
}