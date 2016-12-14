// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// A builder for <see cref="IHost"/>
    /// </summary>
    public class HostBuilder : IHostBuilder
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly List<Action<IServiceCollection>> _configureServicesDelegates;
        private readonly List<Action<ILoggerFactory>> _configureLoggingDelegates;

        private IConfiguration _config;
        private ILoggerFactory _loggerFactory;
        private bool _hostBuilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class.
        /// </summary>
        public HostBuilder()
        {
            _hostingEnvironment = new HostingEnvironment();
            _configureServicesDelegates = new List<Action<IServiceCollection>>();
            _configureLoggingDelegates = new List<Action<ILoggerFactory>>();

            _config = new ConfigurationBuilder()
                .Build();
        }

        public IHostingEnvironment HostingEnvironment => _hostingEnvironment;

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public IHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        public string GetSetting(string key)
        {
            return _config[key];
        }

        /// <summary>
        /// Specify the <see cref="ILoggerFactory"/> to be used by the web host.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to be used.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public IHostBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public IHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            _configureServicesDelegates.Add(configureServices);
            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggerFactory"/>. This may be called multiple times.
        /// </summary>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public IHostBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
        {
            if (configureLogging == null)
            {
                throw new ArgumentNullException(nameof(configureLogging));
            }

            _configureLoggingDelegates.Add(configureLogging);
            return this;
        }

        /// <summary>
        /// Builds the required services and an <see cref="IHost"/> which hosts a web application.
        /// </summary>
        public IHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException(Resources.WebHostBuilder_SingleInstance);
            }
            _hostBuilt = true;

            // Warn about deprecated environment variables
            if (Environment.GetEnvironmentVariable("Hosting:Environment") != null)
            {
                Console.WriteLine("The environment variable 'Hosting:Environment' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
            }

            if (Environment.GetEnvironmentVariable("ASPNET_ENV") != null)
            {
                Console.WriteLine("The environment variable 'ASPNET_ENV' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
            }

            if (Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS") != null)
            {
                Console.WriteLine("The environment variable 'ASPNETCORE_SERVER.URLS' is obsolete and has been replaced with 'ASPNETCORE_URLS'");
            }

            var hostingServices = BuildCommonServices();
            var hostingServiceProvider = hostingServices.BuildServiceProvider();

            var host = new Host(hostingServiceProvider);

            return host;
        }

        private IServiceCollection BuildCommonServices()
        {
            var options = new WebHostOptions(_config);

            var appEnvironment = PlatformServices.Default.Application;
            var contentRootPath = ResolveContentRootPath(options.ContentRootPath, appEnvironment.ApplicationBasePath);
            var applicationName = options.ApplicationName ?? appEnvironment.ApplicationName;

            // Initialize the hosting environment
            _hostingEnvironment.Initialize(applicationName, contentRootPath, options);

            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);

            // The configured ILoggerFactory is added as a singleton here. AddLogging below will not add an additional one.
            if (_loggerFactory == null)
            {
                _loggerFactory = new LoggerFactory();
                services.AddSingleton(provider => _loggerFactory);
            }
            else
            {
                services.AddSingleton(_loggerFactory);
            }

            foreach (var configureLogging in _configureLoggingDelegates)
            {
                configureLogging(_loggerFactory);
            }

            //This is required to add ILogger of T.
            services.AddLogging();

            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticListener>(listener);
            services.AddSingleton<DiagnosticSource>(listener);

            services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();
            services.AddSingleton<HostedServiceExecutor>();
            services.AddOptions();

            services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

            // Ensure object pooling is available everywhere.
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            foreach (var configureServices in _configureServicesDelegates)
            {
                configureServices(services);
            }

            return services;
        }

        private string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }
    }
}
