#region Purpose
// Shared service wiring (OpenTelemetry, health checks, service discovery, HTTP resilience) every ContainerApp calls.
#endregion

#region Design
// Mirrors the stock Aspire ServiceDefaults template — deviations (including reformatting) are avoided so
// upgrades can be diffed against upstream; put repo-specific behavior in the service projects instead.
// Kept in the Microsoft.Extensions.Hosting namespace, as upstream does, so callers need no extra using.
// OTLP export activates only when Aspire injects OTEL_EXPORTER_OTLP_ENDPOINT; alternative exporters
// (Prometheus, Azure Monitor) stay as commented opt-ins requiring extra packages.
// AddMeter(AnalyticsMeters.MeterName) is the one deliberate deviation: this project must register
// the analytics meter for OTLP export without referencing a web feature. The name constant lives
// in TimeWarp.Foundation.Configuration (same home as ServiceNames).
// AddLogRedaction is the second deliberate deviation (task 344): it lives in its own file
// (log-redaction-extensions.cs) so this one stays diffable; AddServiceDefaults calls it first.
// MapDefaultEndpoints is the third deliberate deviation (task 377): upstream maps /health and
// /alive only in Development. Here /alive is mapped in EVERY environment because the ingress
// health route (IngressPaths.WebServerAlivePath) targets web-server's /alive, and a probe path
// that 404s outside Development is no probe. Security reasoning: /alive runs only checks tagged
// "live" (the constant "self" check), so it is cheap, touches no dependency, and cannot be used
// to load a database or downstream service. It is anonymous by design and uses the default
// response writer, which writes only the aggregate status text ("Healthy") — no check names,
// descriptions, exception text, or dependency state. Do not add a custom ResponseWriter or a
// dependency check tagged "live". /health (every check, including dependency checks) stays
// Development-only. Paths are HealthEndpointPaths constants.
#endregion

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
  public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
  {
    builder.AddLogRedaction();

    builder.ConfigureOpenTelemetry();

    builder.AddDefaultHealthChecks();

    builder.Services.AddServiceDiscovery();

    builder.Services.ConfigureHttpClientDefaults(http =>
    {
      // Turn on resilience by default
      http.AddStandardResilienceHandler();

      // Turn on service discovery by default
      http.AddServiceDiscovery();
    });

    return builder;
  }

  public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
  {
    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.IncludeFormattedMessage = true;
      logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
          metrics.AddAspNetCoreInstrumentation()
                  .AddHttpClientInstrumentation()
                  .AddRuntimeInstrumentation()
                  .AddMeter(TimeWarp.Foundation.Configuration.AnalyticsMeters.MeterName);
        })
        .WithTracing(tracing =>
        {
          tracing.AddAspNetCoreInstrumentation()
                  // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                  //.AddGrpcClientInstrumentation()
                  .AddHttpClientInstrumentation();
        });

    builder.AddOpenTelemetryExporters();

    return builder;
  }

  private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
  {
    bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

    if (useOtlpExporter)
    {
      builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
    // builder.Services.AddOpenTelemetry()
    //    .WithMetrics(metrics => metrics.AddPrometheusExporter());

    // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
    //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    //{
    //    builder.Services.AddOpenTelemetry()
    //       .UseAzureMonitor();
    //}

    return builder;
  }

  public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
  {
    builder.Services.AddHealthChecks()
        // Add a default liveness check to ensure app is responsive
        .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

    return builder;
  }

  public static WebApplication MapDefaultEndpoints(this WebApplication app)
  {
    // Only health checks tagged with the "live" tag must pass for app to be considered alive.
    // Every environment, anonymous, status text only (see Design).
    app.MapHealthChecks(TimeWarp.Foundation.Configuration.HealthEndpointPaths.Alive, new HealthCheckOptions
    {
      Predicate = r => r.Tags.Contains("live")
    }).AllowAnonymous();

    // Dependency checks stay Development-only (see Design).
    if (app.Environment.IsDevelopment())
    {
      // All health checks must pass for app to be considered ready to accept traffic after starting
      app.MapHealthChecks(TimeWarp.Foundation.Configuration.HealthEndpointPaths.Health);

      // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
      // app.MapPrometheusScrapingEndpoint();
    }

    return app;
  }
}
