using System;
using Serilog;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Events;
using System.Dynamic;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Threading;
using System.Reflection;
using RabbitMQ.Client.Events;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Log4Rabbit.Receive
{

    public class MyClassBuilder
    {
        AssemblyName asemblyName;
        public MyClassBuilder(string ClassName)
        {
            this.asemblyName = new AssemblyName(ClassName);
        }
        public object CreateObject(string[] PropertyNames, Type[] Types)
        {
            if (PropertyNames.Length != Types.Length)
            {
                Console.WriteLine("The number of property names should match their corresopnding types number");
            }

            TypeBuilder DynamicClass = this.CreateClass();
            this.CreateConstructor(DynamicClass);
            for (int ind = 0; ind < PropertyNames.Count(); ind++)
                CreateProperty(DynamicClass, PropertyNames[ind], Types[ind]);
            Type type = DynamicClass.CreateType();

            return Activator.CreateInstance(type);
        }
        private TypeBuilder CreateClass()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(this.asemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(this.asemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return typeBuilder;
        }
        private void CreateConstructor(TypeBuilder typeBuilder)
        {
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }
        private void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
    internal class HostApplicationLifetimeEventsHosted : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IConfiguration _configuration;
        public HostApplicationLifetimeEventsHosted(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
             => Task.CompletedTask;

        private void OnStarted()
        {

            Console.WriteLine("Start Receive Log!!!!");
            var logFile = new StreamWriter("Log4Rabbit.log", true);
            try
            {
                var hostName = _configuration.GetSection("Log4Rabbit:Host").Value;
                var port = _configuration.GetSection("Log4Rabbit:Port").Value;
                var userName = _configuration.GetSection("Log4Rabbit:UserName").Value;
                var password = _configuration.GetSection("Log4Rabbit:Password").Value;
                var exchange = _configuration.GetSection("Log4Rabbit:Exchange").Value;
                var routeKey = _configuration.GetSection("Log4Rabbit:RouteKey").Value;
                var queueName = _configuration.GetSection("Log4Rabbit:QueueName").Value;
                var virtualHost = _configuration.GetSection("Log4Rabbit:VirtualHost").Value;
                var factory = new ConnectionFactory { HostName = hostName, Port = int.Parse(port), UserName = userName, Password = password, Ssl = new SslOption { Enabled = false }, VirtualHost = virtualHost };
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                channel.ExchangeDeclare(exchange, ExchangeType.Direct, true, false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {

                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    dynamic config = JsonConvert.DeserializeObject<ExpandoObject>(message, new ExpandoObjectConverter());
                    var logData = JsonConvert.DeserializeObject<LogData>(message);

                    LogEventLevel logLevel;
                    Enum.TryParse(logData.Level, out logLevel);
                    if (logLevel == LogEventLevel.Error)
                    {
                        Log
                         .ForContext("ApplicationContext", logData.Properties.ApplicationContext)
                         .ForContext("SendLogTime", logData.Timestamp)
                         .ForContext("MachineName", logData.Properties.MachineName)
                         .ForContext("ErrorId", config.Properties.ErrorId ?? Guid.NewGuid())
                         .Write(logLevel, logData.MessageTemplate, config.Properties);
                    }
                    else
                    {
                        Log
                        .ForContext("ApplicationContext", logData.Properties.ApplicationContext)
                        .ForContext("SendLogTime", logData.Timestamp)
                        .ForContext("MachineName", logData.Properties.MachineName)
                        .Write(logLevel, logData.MessageTemplate, config.Properties);
                    }
                };
                channel.QueueDeclare(queueName, true, false, false);
                channel.QueueBind(queueName, exchange, routeKey);

                channel.BasicConsume(
                    queue: queueName,
                    autoAck: true,
                    consumer: consumer);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                logFile.Write(ex.ToString());
                logFile.FlushAsync();
            }
            // ...
        }

        private void OnStopping()
        {
            // ...
        }

        private void OnStopped()
        {
            // ...
        }
    }
}
