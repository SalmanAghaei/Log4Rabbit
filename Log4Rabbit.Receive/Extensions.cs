using Microsoft.Extensions.DependencyInjection;

namespace Log4Rabbit.Receive
{
    public static  class Extensions
    {
        public static IServiceCollection AddReceiveLog4Rabbit(this IServiceCollection services)=>
            services.AddHostedService<HostApplicationLifetimeEventsHosted>();

    }
}
