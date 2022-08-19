using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Parsing;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args) => await BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.ConfigureServices(services =>
                    {
                        //services.AddSingleton<IGreeter, Greeter>();
                    });
                })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);

        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand(@"$ dotnet run --name 'Joe'"){
                new Option<string>("--name"){
                    IsRequired = true
                }
            };
            //root.Handler = CommandHandler.Create<GreeterOptions, IHost>(Run);
            return new CommandLineBuilder(root);
        }

        // private static void Run(GreeterOptions options, IHost host)
        // {
        //     var serviceProvider = host.Services;
        //     var greeter = serviceProvider.GetRequiredService<IGreeter>();
        //     var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        //     var logger = loggerFactory.CreateLogger(typeof(Program));

        //     var name = options.Name;
        //     logger.LogInformation(GreetEvent, "Greeting was requested for: {name}", name);
        //     greeter.Greet(name);
        // }
    }
}
