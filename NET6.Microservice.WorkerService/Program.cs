using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using NET6.Microservice.Core.OpenTelemetry;
using NET6.Microservice.Messages;
using NET6.Microservice.WorkerService;
using NET6.Microservice.WorkerService.Consumers;
using NET6.Microservice.WorkerService.Services;
using OpenTelemetry;
using OpenTelemetry.Extensions.AzureMonitor;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Exceptions;

// This is required if the collector doesn't expose an https endpoint. By default, .NET
// only allows http2 (required for gRPC) to secure endpoints.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "WorkerService";
    })
    .ConfigureAppConfiguration((host, builder) =>{
        builder.AddEnvironmentVariables();
        //builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureLogging((hostingContext,logging) => {
        logging.ClearProviders();

        var configuration = hostingContext.Configuration;

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithExceptionDetails()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Environment", configuration.GetValue<string>("Environment"))
            .CreateLogger();

        logging.AddSerilog(Log.Logger);

        var resourceBuilder = GetResourceBuilder(hostingContext.HostingEnvironment);
        logging.AddOpenTelemetry(configure =>
        {
            configure.SetResourceBuilder(resourceBuilder)
            .AddOtlpExporter(otlpExporterOptions =>
            {
                hostingContext.Configuration.GetSection("OpenTelemetry:OtelCollector").Bind(otlpExporterOptions);
            });
            configure.IncludeFormattedMessage = true;
            configure.IncludeScopes = true;
            configure.ParseStateValues = true;
        });
    })
    .ConfigureServices((hostingContext, services) =>
    {
        var configuration = hostingContext.Configuration;

        services.AddOptions();
        services.AddSingleton<EmailService>();
        services.AddOptions<MassTransitConfiguration>().Bind(configuration.GetSection("MassTransit"));

        InitMassTransitConfig(services,configuration);

        string[] sources = new string[2] { "OrderConsumer", "MassTransit" };
        string otlpExporterUri = configuration.GetValue<string>("OpenTelemetry:OtelCollector");
        bool isAzureExporter = configuration.GetValue<bool>("OpenTelemetry:IsAzureExporter");
        string azureMonitorTraceExporter = configuration.GetValue<string>("OpenTelemetry:AzureMonitorTraceExporter");

        if (isAzureExporter)
        {
            // Setting role name and role instance
            var resourceAttributes = new Dictionary<string, object> {{ "service.name", "WorkerService" }};
            var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

            Sdk.CreateTracerProviderBuilder()
                .AddSource(sources)
                .SetSampler(new ApplicationInsightsSampler(0.1F))
                .SetResourceBuilder(resourceBuilder)
                .AddAzureMonitorTraceExporter(o => o.ConnectionString = azureMonitorTraceExporter)
                .Build();

            Sdk.CreateMeterProviderBuilder()
                .AddMeter("OTel.AzureMonitor.Demo")
                .AddAzureMonitorMetricExporter(o => o.ConnectionString = azureMonitorTraceExporter)
                .Build();

            LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = azureMonitorTraceExporter);
                });

            });
        }
        else
        {
            OpenTelemetryStartup.InitOpenTelemetryTracing(services, configuration, "Worker", sources, otlpExporterUri);
        }


        services.AddHostedService<Worker>();
    })
    .Build();

static ResourceBuilder GetResourceBuilder(IHostEnvironment hostEnvironment)
{
    // Configure OpenTelemetry service resource details
    // See https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions
    var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
    var entryAssemblyName = entryAssembly?.GetName();
    var versionAttribute = entryAssembly?.GetCustomAttributes(false)
        .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
        .FirstOrDefault();
    var serviceName = entryAssemblyName?.Name;

    var serviceVersion = versionAttribute?.InformationalVersion ?? entryAssemblyName?.Version?.ToString();
    var attributes = new Dictionary<string, object>
    {
        ["host.name"] = Environment.MachineName,
        ["os.description"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        ["deployment.environment"] = hostEnvironment.EnvironmentName.ToLowerInvariant()
    };

    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(serviceName, serviceVersion: serviceVersion)
        .AddTelemetrySdk()
        .AddAttributes(attributes);

    return resourceBuilder;
}
static void InitMassTransitConfig(IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<MassTransitConfiguration>();
    services.Configure<MassTransitConfiguration>(configuration.GetSection("MassTransit"));

    var massTransitConfiguration = configuration.GetSection("MassTransit").Get<MassTransitConfiguration>();

    services.AddMassTransit(configureMassTransit =>
    {
        if (massTransitConfiguration == null)
        {
            throw new ArgumentNullException("MassTransit config is null");
        }

        configureMassTransit.AddConsumer<OrderConsumer>(configureConsumer =>
        {
            configureConsumer.UseConcurrentMessageLimit(2);
        });

        if (massTransitConfiguration.IsUsingAzureServiceBus)
        {
            configureMassTransit.UsingAzureServiceBus((context, configure) =>
            {
                ServiceBusConnectionConfig.ConfigureNodes(configure, massTransitConfiguration.AzureServiceBus);

                // setup Azure queue consumer
                configure.ReceiveEndpoint(massTransitConfiguration.OrderQueue, endpoint =>
                {
                    // all of these are optional!!
                    endpoint.PrefetchCount = 4;

                    // number of "threads" to run concurrently
                    endpoint.MaxConcurrentCalls = 3;

                    endpoint.ConfigureConsumer<OrderConsumer>(context);
                });
            });

        }
        else
        {
            configureMassTransit.UsingRabbitMq((context, configure) =>
            {
                configure.PrefetchCount = 4;

                // Ensures the processor gets its own queue for any consumed messages
                configure.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(true));

                configure.ReceiveEndpoint(massTransitConfiguration.OrderQueue, receive =>
                {
                    receive.ConfigureConsumer<OrderConsumer>(context);
                });

                ServiceBusConnectionConfig.ConfigureNodes(configure, massTransitConfiguration.MessageBusRabbitMQ);
            });
        }
    });

    services.AddMassTransitHostedService();

    // OPTIONAL, but can be used to configure the bus options
    services.AddOptions<MassTransitHostOptions>().Configure(options =>
    {
        // if specified, waits until the bus is started before
        // returning from IHostedService.StartAsync
        // default is false
        options.WaitUntilStarted = true;

        // if specified, limits the wait time when starting the bus
        options.StartTimeout = TimeSpan.FromSeconds(40);

        // if specified, limits the wait time when stopping the bus
        options.StopTimeout = TimeSpan.FromSeconds(60);
    });
}

//await host.Services.GetRequiredService<IBusControl>().StartAsync();
await host.RunAsync();
