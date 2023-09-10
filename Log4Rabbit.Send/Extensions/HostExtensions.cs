using Log4Rabbit.Send.RabbitClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.RabbitMQ;
using Serilog.Sinks.RabbitMQ.Sinks.RabbitMQ;

namespace Log4Rabbit.Send.Extensions
{
    public static class HostExtensions
    {

        public static IHostBuilder UseLog4Rabbit(this IHostBuilder hostBuilder) =>
            hostBuilder.UseSerilog();
        public static void AddLog4Rabbit(this IServiceCollection services, IConfiguration configuration, RabbitMqClientSetting clientSetting,string serivceIdSection)
        {

            var config = new RabbitMQClientConfiguration
            {
                Port = clientSetting.Port == 0 ? 5672 : clientSetting.Port,
                DeliveryMode = RabbitMQDeliveryMode.Durable,
                Exchange = string.IsNullOrEmpty(clientSetting.Exchange) ? "Log4Rabbit_Exchange": clientSetting.Exchange,
                Username = clientSetting.Username,
                Password = clientSetting.Password,
                ExchangeType = "direct",
                RouteKey = string.IsNullOrEmpty(clientSetting.RouteKey) ? "Logging": clientSetting.RouteKey,
                VHost =string.IsNullOrEmpty(clientSetting.VHost) ? "/": clientSetting.VHost,
            };
            config.Hostnames.Add(clientSetting.HostName);
            var serviceId = configuration.GetSection(serivceIdSection).Value ?? "Not Define ServiceId";
            Log.Logger = new LoggerConfiguration()
                         .Enrich.WithProperty("ApplicationContext", serviceId)
                         .Enrich.WithProperty("MachineName", System.Environment.GetEnvironmentVariable("COMPUTERNAME"))
                         .Enrich.FromLogContext()
                         .WriteTo.RabbitMQ(config, new RabbitMQSinkConfiguration() { TextFormatter = new JsonFormatter() })
                         //.ReadFrom.Configuration(configuration)
                         .CreateLogger();
            Log.Information("Start Send Log To Rabbit!!!!");
        }
    }
}
