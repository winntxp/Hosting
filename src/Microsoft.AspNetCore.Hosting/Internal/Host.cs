// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class Host : IHost
    {
        private ApplicationLifetime _applicationLifetime;
        private HostedServiceExecutor _hostedServiceExecutor;
        private readonly IServiceProvider _hostingServiceProvider;

        private ILogger<Host> _logger;
        
        public Host(IServiceProvider hostingServiceProvider)
        {
            if (hostingServiceProvider == null)
            {
                throw new ArgumentNullException(nameof(hostingServiceProvider));
            }

            _hostingServiceProvider = hostingServiceProvider;
        }

        public IServiceProvider Services
        {
            get
            {
                return _hostingServiceProvider;
            }
        }
        
        public virtual void Start()
        {
            HostingEventSource.Log.HostStart();

            _logger = _hostingServiceProvider.GetRequiredService<ILogger<Host>>();
            _logger.Starting();

            _applicationLifetime = _hostingServiceProvider.GetRequiredService<IApplicationLifetime>() as ApplicationLifetime;
            _hostedServiceExecutor = _hostingServiceProvider.GetRequiredService<HostedServiceExecutor>();

            // Fire IApplicationLifetime.Started
            _applicationLifetime?.NotifyStarted();

            // Fire IHostedService.Start
            _hostedServiceExecutor.Start();

            _logger.Started();
        }

        public void Dispose()
        {
            _logger?.Shutdown();

            // Fire IApplicationLifetime.Stopping
            _applicationLifetime?.StopApplication();

            // Fire the IHostedService.Stop
            _hostedServiceExecutor?.Stop();

            (_hostingServiceProvider as IDisposable)?.Dispose();

            // Fire IApplicationLifetime.Stopped
            _applicationLifetime?.NotifyStopped();

            HostingEventSource.Log.HostStop();
        }
    }
}
