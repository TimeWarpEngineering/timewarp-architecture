#region Purpose
// Exports server-side TimeWarp.State action spans to the State DevTools inspector (Development only).
#endregion

#region Design
// Task 335-001: AddExport adds the TimeWarp.State ActivitySource and the DevTools
// exporter to tracing in Development when the AppHost published TIMEWARP_STATE_DEVTOOLS_ENDPOINT.
// It covers server-side stores (prerender, InteractiveServer).
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TimeWarp.State.DevTools.Export;
using TimeWarp.State.Telemetry;

/// <summary>Server-side State DevTools span export.</summary>
internal static class WebServerStateDevTools
{
  /// <summary>
  /// Task 335-001: ships server-side TimeWarp.State action spans (prerender and InteractiveServer
  /// circuits) to the State DevTools inspector. Development only, and only when the AppHost set
  /// TIMEWARP_STATE_DEVTOOLS_ENDPOINT (WithTimeWarpStateDevTools, run mode). Production and every
  /// published target register nothing. Snapshots use a reflection resolver: this is a
  /// non-trimmed server process and a failed snapshot never fails the action.
  /// </summary>
  internal static void AddExport(IHostApplicationBuilder builder)
  {
    if (!builder.Environment.IsDevelopment()
      || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TimeWarpStateDevToolsExport.EndpointEnvironmentVariable)))
    {
      return;
    }

    builder.Services.AddTimeWarpStateTelemetry
    (
      options =>
      {
        options.IncludeSnapshots = true;
        options.JsonSerializerOptions =
          new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
      }
    );
    builder.Services.AddOpenTelemetry()
      .WithTracing
      (
        tracing => tracing
          .AddSource(TimeWarpStateTelemetry.ActivitySourceName)
          .AddTimeWarpStateDevToolsExporter()
      );
  }
}
