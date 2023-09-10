using Log4Rabbit.Receive;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sample
{
    public class Program
    {
        public static int Main(string[] args)
        {
            MyClassBuilder MCB = new MyClassBuilder("Student");
            var myclass = MCB.CreateObject(new string[3] { "ID", "Name", "Address" }, new Type[3] { typeof(int), typeof(string), typeof(string) });
            Type TP = myclass.GetType();

            foreach (PropertyInfo PI in TP.GetProperties())
            {
                Console.WriteLine(PI.Name);
            }

            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            try
            {
                Log.Information("Starting Web Host");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated");
                return 1;

            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
