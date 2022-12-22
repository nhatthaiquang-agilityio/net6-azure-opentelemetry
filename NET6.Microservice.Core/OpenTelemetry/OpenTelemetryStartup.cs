using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Extensions.AzureMonitor;

namespace NET6.Microservice.Core.OpenTelemetry
{
    public class OpenTelemetryStartup
    {
        public static void InitOpenTelemetryTracing(
            IServiceCollection services, IConfiguration configuration, string serviceName,
            string[] sources, string otlpExporterUri = "", IWebHostEnvironment webHostEnvironment = null)
        {
            bool isZipkinExporter = configuration.GetValue<bool>("OpenTelemetry:IsZipkinExporter");
            bool isJaegerExporter = configuration.GetValue<bool>("OpenTelemetry:IsJaegerExporter");
            bool isAzureExporter = configuration.GetValue<bool>("OpenTelemetry:IsAzureExporter");
            string azureMonitorTraceExporter = configuration.GetValue<string>("OpenTelemetry:AzureMonitorTraceExporter");


            services.AddOpenTelemetryTracing(builder =>
            {
                builder
                    .AddSource(serviceName)
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Enrich = Enrich;
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                if (isZipkinExporter)
                {
                    builder.AddZipkinExporter(options =>
                    {
                        var zipkinURI = configuration.GetValue<string>("OpenTelemetry:ZipkinURI");
                        if (!string.IsNullOrEmpty(zipkinURI))
                        {
                            options.Endpoint = new Uri(zipkinURI);
                        }
                    });
                }

                if (isJaegerExporter)
                {
                    builder.AddJaegerExporter(options =>
                    {
                        var agentHost = configuration.GetValue<string>("OpenTelemetry:JaegerHost");
                        var agentPort = configuration.GetValue<int>("OpenTelemetry:JaegerPort");

                        if (!string.IsNullOrEmpty(agentHost) && agentPort > 0)
                        {
                            options.AgentHost = agentHost;
                            options.AgentPort = agentPort;
                        }
                    });
                }

                if (sources.Any())
                {
                    builder.AddSource(sources);
                }

                if (webHostEnvironment != null && webHostEnvironment.IsDevelopment())
                {
                    builder.AddConsoleExporter();
                }

                builder.AddOtlpExporter(options => options.Endpoint = new Uri(otlpExporterUri));
            });
        }

        public static void AddOpenTelemetryAzureLogging(WebApplicationBuilder builder, string azureMonitorTraceExporter)
        {
            // Configure logging
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.AddAzureMonitorLogExporter(o => o.ConnectionString = azureMonitorTraceExporter);
            });
        }

        public static void AddOpenTelemetryLogging(WebApplicationBuilder builder, string otlpExporterUri)
        {
            // Configure logging
            builder.Logging.AddOpenTelemetry(builderOpenTelemetry =>
            {
                builderOpenTelemetry.IncludeFormattedMessage = true;
                builderOpenTelemetry.IncludeScopes = true;
                builderOpenTelemetry.ParseStateValues = true;
                builderOpenTelemetry.AddOtlpExporter(options => options.Endpoint = new Uri(otlpExporterUri));
            });
        }

        public static void AddOpenTelemetryMetrics(IServiceCollection services, string serviceName, string otlpExporterUri)
        {
            // Configure metrics
            services.AddOpenTelemetryMetrics(builder =>
            {
                builder.AddHttpClientInstrumentation();
                builder.AddAspNetCoreInstrumentation();
                builder.AddMeter(serviceName);
                builder.AddOtlpExporter(options => options.Endpoint = new Uri(otlpExporterUri));
            });
        }
        private static void Enrich(Activity activity, string eventName, object obj)
        {
            if (obj is HttpRequest request)
            {
                var context = request.HttpContext;
                activity.AddTag("http.scheme", request.Scheme);
                activity.AddTag("http.client_ip", context.Connection.RemoteIpAddress);
                activity.AddTag("http.request_content_length", request.ContentLength);
                activity.AddTag("http.request_content_type", request.ContentType);
            }
            else if (obj is HttpResponse response)
            {
                activity.AddTag("http.response_content_length", response.ContentLength);
                activity.AddTag("http.response_content_type", response.ContentType);
            }
        }
    }
}