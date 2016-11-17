// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    [EventSource(Name = "Microsoft-AspNetCore-Hosting")]
    public sealed class HostingEventSource : EventSource
    {
        public static readonly HostingEventSource Log = new HostingEventSource();

#if NETSTANDARD1_5
        private EventCounter _requestCounter = null;
        private EventCounter _successfulRequestCounter = null;
        private EventCounter _failedRequestCounter = null;
        private EventCounter _requestExecutionTimeCounter = null;
#endif

        private HostingEventSource()
        {
#if NETSTANDARD1_5
            _requestCounter = new EventCounter("Request", this);
            _successfulRequestCounter = new EventCounter("SuccessfulRequest", this);
            _failedRequestCounter = new EventCounter("FailedRequest", this);
            _requestExecutionTimeCounter = new EventCounter("RequestExecutionTime", this);
#endif
        }

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.

        [Event(1, Level = EventLevel.Informational)]
        public void HostStart()
        {
            WriteEvent(1);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void HostStop()
        {
            WriteEvent(2);
        }

        [Event(3, Level = EventLevel.Informational)]
        public void RequestStart(string method, string path)
        {
#if NETSTANDARD1_5
            _requestCounter.WriteMetric(1);
#endif
            if (IsEnabled())
            {
                RequestStart(
                    context.TraceIdentifier,
                    context.Request.Protocol,
                    context.Request.Method,
                    context.Request.ContentType ?? string.Empty,
                    context.Request.ContentLength.HasValue ? context.Request.ContentLength.Value.ToString() : string.Empty,
                    context.Request.Scheme,
                    context.Request.Host.ToString(),
                    context.Request.PathBase,
                    context.Request.Path,
                    context.Request.QueryString.ToString());
            }
            WriteEvent(3, method, path);
        }

        [Event(4, Level = EventLevel.Informational)]
        public void RequestStop()
        {
#if NETSTANDARD1_5
            if (exception == null)
            {
                _successfulRequestCounter.WriteMetric(1);
            }
            else
            {
                _failedRequestCounter.WriteMetric(1);
            }

            if (endTimestamp != 0)
            {
                _requestExecutionTimeCounter.WriteMetric(endTimestamp - startTimestamp);
            }
#endif
            if (IsEnabled())
            {
                RequestStop(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? string.Empty,
                    context.TraceIdentifier,
                    exception == null ? string.Empty : exception.ToString());
            }
            WriteEvent(4);
        }

        [Event(5, Level = EventLevel.Error)]
        public void UnhandledException()
        {
            WriteEvent(5);
        }
    }
}
