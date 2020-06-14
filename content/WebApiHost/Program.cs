using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApiHost
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                        .ConfigureWebHostDefaults(builder => 
            {
                builder.UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                        .ConfigureAppConfiguration(ConfigureAppConfiguration)
                        .ConfigureLogging(ConfigureLogging)
                        .ConfigureKestrel(ConfigureKestrel)
                        .UseStartup<Startup>();
            });
        }

        private static void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder logging)
        {
            logging.AddConfiguration(context.Configuration.GetSection("Logging"));

            logging.AddLoggly(context.Configuration.GetSection("Loggly"));

            if (context.HostingEnvironment.IsDevelopment())
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            }
        }

        private static void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
        {
            var hostingEnvironment = context.HostingEnvironment;

            // Creates a default sets of settings that can be overridden via file or environment variable
            var settings = new Dictionary<string, string>
            {
                ["Loggly:ApplicationName"] = "WebApiHost",
                ["Loggly:Environment"] = context.HostingEnvironment.EnvironmentName,

                ["Loggly:ApiKey"] = "do-not-touch-this", // this is a fallback value. The real value should be provided via environment variables
                ["JWT:SecretKey"] = "do-not-touch-this", // this is a fallback value. The real value should be provided via environment variables
            };

            builder.AddInMemoryCollection(settings);
            builder.AddJsonFile("appsettings.json", true, true);
            builder.AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", true, true);
            builder.AddEnvironmentVariables();
        }

        private static void ConfigureKestrel(WebHostBuilderContext context, KestrelServerOptions options)
        {

        }
    }
}
