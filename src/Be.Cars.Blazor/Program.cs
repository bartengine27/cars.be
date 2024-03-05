using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace Be.Cars.Blazor;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
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
            .CreateLogger();

        try
        {
            Log.Information("Starting web host.");
            var builder = WebApplication.CreateBuilder(args);            
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<CarsBlazorModule>();
            //add telemetry
            //https://community.abp.io/posts/asp.net-core-metrics-with-.net-8.0-1xnw1apc
            //https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics?view=aspnetcore-8.0
            //https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md
            builder.Services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {                    
                    builder.AddAspNetCoreInstrumentation();
                    //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                    builder.AddMeter("Microsoft.AspNetCore.Hosting");
                    //.net8 only https://github.com/open-telemetry/opentelemetry-dotnet/pull/4934
                    builder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                    builder.AddView("http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                        });
                    //builder.AddAspNetCoreInstrumentation();
                    builder.AddPrometheusExporter();
                    builder.AddConsoleExporter();
                });
            var app = builder.Build();            
            //app.UseOpenTelemetryPrometheusScrapingEndpoint(
            //    context => context.Request.Path == builder.Configuration["ApplicationInsights:Path"]
            //    /*&& context.Connection.LocalPort == 5001*/);
            app.MapPrometheusScrapingEndpoint();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
