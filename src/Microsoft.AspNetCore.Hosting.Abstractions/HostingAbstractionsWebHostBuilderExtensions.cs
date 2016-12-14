// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class HostingAbstractionsWebHostBuilderExtensions
    {
        private static readonly string ServerUrlsSeparator = ";";

        /// <summary>
        /// Use the given configuration settings on the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> containing settings to be used.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseConfiguration(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            foreach (var setting in configuration.AsEnumerable())
            {
                hostBuilder.UseSetting(setting.Key, setting.Value);
            }

            return hostBuilder;
        }

        /// <summary>
        /// Set whether startup errors should be captured in the configuration settings of the web host.
        /// When enabled, startup exceptions will be caught and an error page will be returned. If disabled, startup exceptions will be propagated.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="captureStartupErrors"><c>true</c> to use startup error page; otherwise <c>false</c>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CaptureStartupErrors(this IHostBuilder hostBuilder, bool captureStartupErrors)
        {
            return hostBuilder.UseSetting(WebHostDefaults.CaptureStartupErrorsKey, captureStartupErrors ? "true" : "false");
        }

        /// <summary>
        /// Specify the assembly containing the startup type to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="startupAssemblyName">The name of the assembly containing the startup type.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseStartup(this IHostBuilder hostBuilder, string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }


            return hostBuilder
                    .UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
                    .UseSetting(WebHostDefaults.StartupAssemblyKey, startupAssemblyName);
        }

        /// <summary>
        /// Specify the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="server">The <see cref="IServer"/> to be used.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseServer(this IHostBuilder hostBuilder, IServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            return hostBuilder.ConfigureServices(services =>
            {
                // It would be nicer if this was transient but we need to pass in the
                // factory instance directly
                services.AddSingleton(server);
            });
        }

        /// <summary>
        /// Specify the environment to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="environment">The environment to host the application in.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseEnvironment(this IHostBuilder hostBuilder, string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return hostBuilder.UseSetting(WebHostDefaults.EnvironmentKey, environment);
        }

        /// <summary>
        /// Specify the content root directory to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="contentRoot">Path to root directory of the application.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseContentRoot(this IHostBuilder hostBuilder, string contentRoot)
        {
            if (contentRoot == null)
            {
                throw new ArgumentNullException(nameof(contentRoot));
            }

            return hostBuilder.UseSetting(WebHostDefaults.ContentRootKey, contentRoot);
        }

        /// <summary>
        /// Specify the webroot directory to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="webRoot">Path to the root directory used by the web server.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseWebRoot(this IHostBuilder hostBuilder, string webRoot)
        {
            if (webRoot == null)
            {
                throw new ArgumentNullException(nameof(webRoot));
            }

            return hostBuilder.UseSetting(WebHostDefaults.WebRootKey, webRoot);
        }

        /// <summary>
        /// Specify the urls the web host will listen on.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="urls">The urls the hosted application will listen on.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseUrls(this IHostBuilder hostBuilder, params string[] urls)
        {
            if (urls == null)
            {
                throw new ArgumentNullException(nameof(urls));
            }

            return hostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(ServerUrlsSeparator, urls));
        }

        /// <summary>
        /// Start the web host and listen on the specified urls.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to start.</param>
        /// <param name="urls">The urls the hosted application will listen on.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHost Start(this IHostBuilder hostBuilder, params string[] urls)
        {
            var host = hostBuilder.UseUrls(urls).Build();
            host.Start();
            return host;
        }
    }
}
