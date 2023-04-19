using MassTransit;
using MassTransit.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sample;
using Sample.Worker.Consumers;
using Sample.Worker.StateMachines;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilogConfiguration()
    .UseMassTransitConfiguration(x =>
    {
        x.AddConsumer<SubmitOrderConsumer, SubmitOrderConsumerDefinition>();
        x.AddConsumer<ValidationConsumer>();

        x.AddSagaStateMachine<OrderStateMachine, OrderState, OrderStateDefinition>()
            .InMemoryRepository();
    })
    .ConfigureAppConfiguration((hostingContext, config) =>
        {
            var env = hostingContext.HostingEnvironment;
            config.SetBasePath(env.ContentRootPath);
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
            config.AddEnvironmentVariables();
        })
    .ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;
            services.AddOpenTelemetry().ConfigureResource(x => x.AddService("oteltest.worker"))
                .WithMetrics(builder =>
                        builder.AddOtlpExporter(c => {
                            c.ExportProcessorType = ExportProcessorType.Batch;
                            c.Endpoint = new Uri(configuration.GetConnectionString("OtelEndpoint"));
                            c.Protocol = OtlpExportProtocol.Grpc;
                        })
                            .AddConsoleExporter()
                )
                .WithTracing(builder => builder
                        .AddSource(DiagnosticHeaders.DefaultListenerName) 
                        .AddOtlpExporter(c => {
                            c.ExportProcessorType = ExportProcessorType.Batch;
                            c.Endpoint = new Uri(configuration.GetConnectionString("OtelEndpoint"));
                            c.Protocol = OtlpExportProtocol.Grpc;
                        })
                        .AddConsoleExporter()
                        // .AddJaegerExporter(c =>
                        // {
                        //     c.AgentHost = "localhost";
                        //     c.AgentPort = 6831;
                        // })
                );
        })
    .Build();

await host.RunAsync();