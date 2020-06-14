//#if (AddNybusBridge)
using Amazon.SimpleNotificationService;
using EMG.Utilities;
//#endif
using EMG.Extensions.AspNetCore;
//#if (AddWcfDiscovery)
using System.ServiceModel;
//#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//#if (AddNybus)
using Nybus;
//#endif

namespace WebApiHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(ConfigureMvc);

            // Adds support for ASP.NET Core health checks: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-3.1
            services.AddHealthChecks(); 

            // Adds support for JWT authentication
            services.AddJwtAuthentication(Configuration)
                    
                    // Extracts the authentication data from the form data of a POST request
                    .AddFormUserExtractor() 
                    
                    // Matches incoming credentials with credentials fetched from the configuration values
                    .AddBasicUserAuthenticator() 

                    // If the delegate returns true, adds an authentication filter at global level that forbids anonymous requests
                    .RequireAuthentication(() => HostingEnvironment.IsProduction()); 
//#if (AddNybus)
            
            // Configures Nybus to use RabbitMQ engine and fetch settings from the configuration values
            services.AddNybus(nybus =>
            {
                nybus.UseConfiguration(Configuration);
                
                nybus.UseRabbitMqBusEngine(rabbitMq =>
                {
                    rabbitMq.UseConfiguration();
                });
            });

            // Configures the NybusHostedService so that Nybus is started when the web application is started
            services.AddHostedService<NybusHostedService>();
//#endif
//#if (AddWcfDiscovery)
            // Configures the WCF Discovery from the current configuration
            services.AddServiceDiscoveryAdapter();
            services.ConfigureServiceDiscovery(Configuration.GetSection("Discovery"));
            services.ConfigureServiceDiscovery(options => options.ConfigureDiscoveryAdapterBinding = binding => binding.Security.Mode = SecurityMode.None);
            services.AddBindingCustomization(binding => binding.Security.Mode = SecurityMode.None);
//#endif
//#if (AddNybusBridge || ConfigureAWS)

            // Configures AWS using the configuration values
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
//#endif
//#if (AddNybusBridge)
            
            // Registers the SNS client
            services.AddAWSService<IAmazonSimpleNotificationService>();

            services.AddSingleton<INybusBridge, SnsNybusBridge>();
//#endif
            
            void ConfigureMvc(MvcOptions options)
            {
                // Adds support for friendly error when an MVC action throws an uncaught exception
                options.AddExceptionHandlerFilter();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseRouting();

            // Uses JWT authentication flow
            app.UseJwtAuthentication();

            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints => 
            {
                // Requests matching "/health" are forwarded to the health check engine
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    AllowCachingResponses = false
                });

                endpoints.MapControllers();
            });
        }
    }
}
