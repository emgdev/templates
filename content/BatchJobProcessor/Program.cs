using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EMG
{
    class Program
    {
        static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        // This template uses McMaster.Extensions.CommandUtils for handling parameters from the command line
        // See example: https://github.com/natemcmaster/CommandLineUtils/blob/master/docs/samples/attributes/Program.cs

        [Argument(0, "First", "First argument")]
        public string FirstRequiredArgument { get; }

        [Argument(1, "Second", "Second argument")]
        public int SecondRequiredArgument { get; }

        [Option(CommandOptionType.SingleValue, LongName = "optional-argument", ShortName = "oa", Description = "Optional argument")]
        public string OptionalArgument { get; }

        private async Task OnExecuteAsync(CommandLineApplication app)
        {
            var hostingConfiguration = CreateHostingConfiguration();
            var configuration = CreateConfiguration(hostingConfiguration);

            var services = new ServiceCollection();

            ConfigureLogging(services, configuration);
            ConfigureApplication(services, configuration);

            var serviceProvider = services.BuildServiceProvider();

            var processor = serviceProvider.GetRequiredService<JobProcessor>();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var parameters = new JobParameters
            {
                FirstArgument = FirstRequiredArgument,
                SecondArgument = SecondRequiredArgument,
                OptionalArgument = OptionalArgument
            };

            logger.LogInformation(parameters, s => $"Starting job");

            await processor.ExecuteAsync(parameters).ConfigureAwait(false);

            logger.LogInformation(parameters, s => $"Job complete");
        }

        static IConfigurationRoot CreateHostingConfiguration()
        {
            var settings = new Dictionary<string, string>
            {

            };

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            builder.AddInMemoryCollection(settings);
            builder.AddJsonFile("hostsettings.json", true, false);

            return builder.Build();
        }

        static IConfigurationRoot CreateConfiguration(IConfigurationRoot hostingConfiguration)
        {
            var environmentName = hostingConfiguration["Environment"] ?? "Development";

            var settings = new Dictionary<string, string>
            {
                //#if (AddLoggly)
                ["Loggly:ApplicationName"] = "EMG BatchJobProcessor",
                ["Loggly:ApiKey"] = "test"
                //#endif
            };

            var builder = new ConfigurationBuilder();
            builder.AddConfiguration(hostingConfiguration);
            builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            builder.AddInMemoryCollection(settings);
            builder.AddJsonFile("appsettings.json", true, false);
            builder.AddJsonFile($"appsettings.{environmentName}.json", true, false);
            builder.AddEnvironmentVariables();

            return builder.Build();
        }

        static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

                //#if (AddLoggly)
                loggingBuilder.AddLoggly(configuration.GetSection("Loggly"));
                //#endif

                loggingBuilder.AddAWSProvider();

                loggingBuilder.AddConsole();
            });
        }

        static void ConfigureApplication(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<JobProcessor>();

            //#if (ConfigureAWS)
            services.AddDefaultAWSOptions(configuration.GetAWSOptions());
            //#endif
        }

    }


}
