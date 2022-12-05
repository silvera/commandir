using Commandir.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Commandir.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddCommandirBaseServices(this IServiceCollection services)
    {
        services.AddSingleton<IActionProvider, ActionProvider>();
        services.AddSingleton<IParameterProvider, ParameterProvider>();
        return services;
    }

    public static IServiceCollection AddCommandirDataServices<TCommandData>(this IServiceCollection services, ICommandDataProvider<TCommandData> commandDataProvider)
    where TCommandData : ICommandData
    {
        services.AddSingleton<ICommandDataProvider<TCommandData>>(sp => commandDataProvider);
        services.AddSingleton<ICancellationTokenProvider, CommandLineCommandDataProvider>();
        services.AddSingleton<IDynamicCommandDataProvider, CommandLineCommandDataProvider>();
        return services;
    }
}