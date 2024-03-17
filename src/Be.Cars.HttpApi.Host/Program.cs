using System;
using System.Threading.Tasks;
using Be.Cars.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Events;

namespace Be.Cars;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string otlpEndpoint = builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317");
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())            
            .WriteTo.OpenTelemetry(otlpOptions =>
                {
                    //otlpOptions.Endpoint = "http://127.0.0.1:4317/";
                    otlpOptions.Endpoint = otlpEndpoint;
                    otlpOptions.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                }
            )
            .CreateLogger();

        try
        {
            Log.Information("Starting Be.Cars.HttpApi.Host.");
//            var builder = WebApplication.CreateBuilder(args);

            //add forward headers middleware, see https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-8.0
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });


            builder.Host.AddAppSettingsSecretsJson()
            .UseAutofac()
            .UseSerilog();
            await builder.AddApplicationAsync<CarsHttpApiHostModule>();
            

            //add telemetry
            //https://community.abp.io/posts/asp.net-core-metrics-with-.net-8.0-1xnw1apc
            //https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics?view=aspnetcore-8.0
            //https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md
            builder.Services.AddSingleton<CustomMetrics>();            
            builder.Services.AddOpenTelemetry()
            .WithMetrics(builder =>
                {
                    builder.AddAspNetCoreInstrumentation();
                    //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                    builder.AddMeter("Microsoft.AspNetCore.Hosting");
                    //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                    builder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                    builder.AddMeter(CustomMetrics.Name);
                    builder.AddView("http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                        });
                    //builder.AddAspNetCoreInstrumentation();
                    //TODO https://opentelemetry.io/docs/collector/
                    //builder.AddOtlpExporter();
                    builder.AddPrometheusExporter();
                    builder.AddConsoleExporter();
                }
            );

            var app = builder.Build();
            app.UseForwardedHeaders();
            app.MapPrometheusScrapingEndpoint();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            if (ex is HostAbortedException)
            {
                throw;
            }
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
