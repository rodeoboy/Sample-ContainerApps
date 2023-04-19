using MassTransit.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sample;

var builder = WebApplication.CreateBuilder(args);
 
builder.Host
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
        
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddOpenTelemetry().ConfigureResource(x => x.AddService("oteltest.api"))
            .WithMetrics(builder =>
                builder.AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddConsoleExporter()
                    .AddOtlpExporter(c => {
                        c.ExportProcessorType = ExportProcessorType.Batch;
                        c.Endpoint = new Uri(configuration.GetConnectionString("OtelEndpoint"));
                        c.Protocol = OtlpExportProtocol.Grpc;
                    })
            )
            .WithTracing(builder => builder.AddAspNetCoreInstrumentation()
                .AddSource(DiagnosticHeaders.DefaultListenerName) 
                .AddConsoleExporter()
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(c => {
                    c.ExportProcessorType = ExportProcessorType.Batch;
                    c.Endpoint = new Uri(configuration.GetConnectionString("OtelEndpoint"));
                    c.Protocol = OtlpExportProtocol.Grpc;
                })
                // .AddJaegerExporter(c =>
                // {
                //     c.AgentHost = "localhost";
                //     c.AgentPort = 6831;
                // })
            );
    });

builder.Host.UseSerilogConfiguration();

builder.Host.UseMassTransitConfiguration(configureBus: (_, bus) => bus.AutoStart = true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();


public partial class Program
{
}