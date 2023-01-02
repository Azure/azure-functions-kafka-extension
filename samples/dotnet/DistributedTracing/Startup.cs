using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System.Diagnostics;
using OpenTelemetry.Exporter;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(EventHub.Startup))]
namespace DistributedTracing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var serviceName = "Kafka.Functions";
            var serviceVersion = "1.0.0";

            var openTelemetry = Sdk.CreateTracerProviderBuilder()
                .AddSource("Microsoft.Azure.WebJobs.Host")
                .AddSource("Microsoft.Azure.WebJobs.Extensions.Kafka")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
                .AddAzureMonitorTraceExporter(o =>
                {
                    o.ConnectionString = "";
                })
                .AddZipkinExporter()
                .Build();
            builder.Services.AddSingleton(openTelemetry);
            AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
        }
    }
}