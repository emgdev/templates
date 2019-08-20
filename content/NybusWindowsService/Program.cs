﻿using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using EMG.Common;
using EMG.NybusWindowsService.Installers;
using Loggly.Config;
using Microsoft.Extensions.Configuration;
using Nybus.Container;
using Nybus.Logging;
using Topshelf;
using Topshelf.CastleWindsor;

namespace EMG.NybusWindowsService
{
    class Program
    {
        public static readonly string ServiceName = "EMG.NybusWindowsService";

        static void Main(string[] args)
        {
            using (var container = CreateContainer())
            {
                SetUpLoggly(container);

                var loggerFactory = container.Resolve<ILoggerFactory>();
                var logger = loggerFactory.CreateCurrentClassLogger();

                var service = HostFactory.New(cfg =>
                {
                    cfg.UseWindsorContainer(container);

                    cfg.Service<NybusService>(svc =>
                    {
                        svc.BeforeStartingService(sc => sc.RequestAdditionalTime(TimeSpan.FromMinutes(1)));
                        svc.BeforeStoppingService(sc => sc.RequestAdditionalTime(TimeSpan.FromMinutes(1)));

                        svc.ConstructUsingWindsorContainer();

                        svc.WhenStarted(host =>
                        {
                            logger.LogInformation($"Starting {ServiceName}");
                            host.Start();
                            logger.LogInformation($"{ServiceName} started");
                        });

                        svc.WhenStopped(host =>
                        {
                            logger.LogInformation($"Stopping {ServiceName}");
                            host.Stop();
                            logger.LogInformation($"{ServiceName} stopped");
                        });
                    });

                    cfg.SetDisplayName("EMG NybusWindowsService");

                    cfg.SetServiceName(ServiceName);

                    cfg.SetDescription("A service running Nybus");

                    cfg.EnableServiceRecovery(rc => rc.RestartService(1).RestartService(5).RestartService(10).SetResetPeriod(1));

                    cfg.RunAsLocalService();
                    cfg.StartAutomaticallyDelayed();
                    cfg.SetStopTimeout(TimeSpan.FromMinutes(5));
                });

                try
                {
                    service.Run();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }

                container.Release(loggerFactory);
            }
        }

        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();

            container.AddConfiguration(
                b => b.AddJsonFile("appsettings.json", true),
                b => b.AddEnvironmentVariables()
            );

            // Check https://app.assembla.com/spaces/studentum/git-8/source/master/Configuration/CastleWindsor/README.md for usages

            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
            container.Install(new DefaultHandlerInstaller(Classes.FromAssemblyInThisApplication()));

            container.Install(FromAssembly.InThisApplication());

            return container;
        }

        private static void SetUpLoggly(IWindsorContainer container)
        {
            var configuration = container.Resolve<IConfigurationRoot>();
            var options = container.Resolve<LogglyOptions>();

            var instance = LogglyConfig.Instance;
            instance.ApplicationName = ServiceName;

            instance.Transport.EndpointHostname = options.EndpointHostname;
            instance.Transport.EndpointPort = options.EndpointPort;
            instance.Transport.LogTransport = options.LogTransport;
            instance.CustomerToken = options.CustomerToken;
            instance.TagConfig.Tags.Add(configuration.GetValue<string>("Environment") ?? "Development");
        }

    }
}
