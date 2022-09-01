using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Commandir.New
{
    public static class ActionExtensions
    {
        public static IServiceCollection AddActions(this IServiceCollection services)
        {
            ActionTypeRegistry actionTypeRegistry = new ActionTypeRegistry();
            services.AddSingleton(actionTypeRegistry);
            services.AddTransient<New.ActionContext>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach(Type type in assembly.GetExportedTypes()) 
            {
                Type actionType = typeof(Action);
                if(type.IsAssignableTo(actionType) && type != actionType)
                {
                    services.AddTransient(type);
                    actionTypeRegistry.RegisterType(type.Name!, type);
                }
            }

            return services;
        }
    }
}
