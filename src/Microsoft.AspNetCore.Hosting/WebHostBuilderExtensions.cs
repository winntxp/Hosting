using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        public static IHostBuilder UseWebApplication(this IHostBuilder builder, Action<WebApplicationBuilder> configure)
        {
            return builder.ConfigureServices(services =>
            {
                var webApplication = new WebApplicationBuilder(services);
                configure(webApplication);

                // TODO: Configuration
                var configuration = new ConfigurationBuilder().Build();
                var options = new WebHostOptions();

                if (!string.IsNullOrEmpty(options.StartupAssembly))
                {
                    try
                    {
                        var startupType = StartupLoader.FindStartupType(options.StartupAssembly, builder.HostingEnvironment.EnvironmentName);

                        if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                        {
                            services.AddSingleton(typeof(IStartup), startupType);
                        }
                        else
                        {
                            services.AddSingleton(typeof(IStartup), sp =>
                            {
                                var hostingEnvironment = sp.GetRequiredService<IHostingEnvironment>();
                                var methods = StartupLoader.LoadMethods(sp, startupType, hostingEnvironment.EnvironmentName);
                                return new ConventionBasedStartup(methods);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        var capture = ExceptionDispatchInfo.Capture(ex);
                        services.AddSingleton<IStartup>(_ =>
                        {
                            capture.Throw();
                            return null;
                        });
                    }
                }

                // Conjure up a RequestServices
                services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

                services.AddSingleton<IHostedService>(sp =>
                {
                    return new WebService(services, sp, options, configuration);
                });
            });
        }
    }

    public class WebApplicationBuilder
    {
        public WebApplicationBuilder(IServiceCollection services)
        {
            Services = services;
        }

        IServiceCollection Services { get; }

        public void UseStartup<TStartup>()
        {
            // Services.AddSingleton<IStartup, TStartup>();
        }
    }
}
