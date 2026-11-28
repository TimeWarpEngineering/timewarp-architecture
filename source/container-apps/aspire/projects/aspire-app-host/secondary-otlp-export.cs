#region Purpose
// Forwards an optional secondary OTLP collector to every run-mode project so dashboard export stays primary.
#endregion

#region Design
// Task 536-001. Aspire writes the dashboard collector onto each child's OTEL_EXPORTER_OTLP_ENDPOINT
// through an environment callback. It does not replace this process's variables. A second collector
// (SigNoz, or the shell's OTEL_EXPORTER_OTLP_ENDPOINT) is forwarded on Observability__SecondaryOtlp__*
// and ServiceDefaults registers a named exporter for it. Forward runs only in run mode, after every
// project resource exists, so publish and deploy manifests never contain those keys.
// Observability:SigNoz:OtlpEndpoint wins when it is set. It must be an absolute http or https URL;
// anything else throws and names the key. Optional Observability:SigNoz:OtlpHeaders and
// Observability:SigNoz:OtlpProtocol travel with that explicit endpoint. A protocol that is set and is
// neither grpc nor http/protobuf throws and names the key. Observability:SecondaryOtlp:Disabled=true
// suppresses only the inherited-environment fallback; an explicit endpoint still forwards.
// With no explicit endpoint, the process environment's OTEL_EXPORTER_OTLP_ENDPOINT (and the matching
// HEADERS and PROTOCOL variables) is the fallback. It is used only when that value is an absolute http
// or https URL and it is not the dashboard OTLP URL (DOTNET_DASHBOARD_OTLP_ENDPOINT_URL or
// ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL, from the process environment or configuration). Comparison uses
// scheme, host, port, and path, ignoring case and a trailing slash. A blank or invalid inherited
// endpoint is skipped. An inherited protocol that is set and is neither grpc nor http/protobuf throws
// and names OTEL_EXPORTER_OTLP_PROTOCOL.
// Headers and protocol are copied only when the chosen source set them. The forwarded keys never
// replace OTEL_EXPORTER_OTLP_ENDPOINT. The declared projects are the api, grpc, and web servers and
// the run-mode ingress, each under its template flag.
#endregion

namespace TimeWarp.Architecture.Aspire;

/// <summary>Run-mode secondary OTLP export beside the Aspire dashboard (task 536-001).</summary>
internal static class SecondaryOtlpExport
{
  private const string ExplicitEndpointKey = "Observability:SigNoz:OtlpEndpoint";
  private const string ExplicitHeadersKey = "Observability:SigNoz:OtlpHeaders";
  private const string ExplicitProtocolKey = "Observability:SigNoz:OtlpProtocol";
  private const string DisabledKey = "Observability:SecondaryOtlp:Disabled";
  private const string InheritedEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";
  private const string InheritedHeadersVariable = "OTEL_EXPORTER_OTLP_HEADERS";
  private const string InheritedProtocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";
  private const string EndpointEnvironmentName = "Observability__SecondaryOtlp__Endpoint";
  private const string HeadersEnvironmentName = "Observability__SecondaryOtlp__Headers";
  private const string ProtocolEnvironmentName = "Observability__SecondaryOtlp__Protocol";

  private static readonly string[] DashboardKeys =
  [
    "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL",
    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL",
  ];

  /// <summary>
  /// Copies the resolved secondary collector onto each declared project resource. No-op outside run mode.
  /// </summary>
  internal static void Forward(IDistributedApplicationBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);
    if (!builder.ExecutionContext.IsRunMode || !TryResolve(builder, out SecondaryOtlpTarget target))
    {
      return;
    }

    foreach (string name in ProjectNames())
    {
      ApplyTo(builder, name, target);
    }
  }

  private static bool TryResolve(IDistributedApplicationBuilder builder, out SecondaryOtlpTarget target)
  {
    target = default;
    string? configured = builder.Configuration[ExplicitEndpointKey];
    if (!string.IsNullOrWhiteSpace(configured))
    {
      string endpoint = configured.Trim();
      if (!IsAbsoluteHttp(endpoint))
      {
        throw new InvalidOperationException(
          $"{ExplicitEndpointKey} '{endpoint}' must be an absolute http or https URL.");
      }

      target = new SecondaryOtlpTarget(
        endpoint,
        BlankToNull(builder.Configuration[ExplicitHeadersKey]),
        RequiredProtocol(builder.Configuration[ExplicitProtocolKey], ExplicitProtocolKey));
      return true;
    }

    if (string.Equals(builder.Configuration[DisabledKey]?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    string? inherited = Environment.GetEnvironmentVariable(InheritedEndpointVariable);
    if (string.IsNullOrWhiteSpace(inherited))
    {
      return false;
    }

    string inheritedEndpoint = inherited.Trim();
    if (!IsAbsoluteHttp(inheritedEndpoint) || MatchesDashboard(builder, inheritedEndpoint))
    {
      return false;
    }

    target = new SecondaryOtlpTarget(
      inheritedEndpoint,
      BlankToNull(Environment.GetEnvironmentVariable(InheritedHeadersVariable)),
      RequiredProtocol(Environment.GetEnvironmentVariable(InheritedProtocolVariable), InheritedProtocolVariable));
    return true;
  }

  private static void ApplyTo(IDistributedApplicationBuilder builder, string name, SecondaryOtlpTarget target)
  {
    if (!builder.TryCreateResourceBuilder(name, out IResourceBuilder<DotnetProjectResource>? project) || project is null)
    {
      return;
    }

    project.WithEnvironment(EndpointEnvironmentName, target.Endpoint);
    if (target.Headers is not null)
    {
      project.WithEnvironment(HeadersEnvironmentName, target.Headers);
    }

    if (target.Protocol is not null)
    {
      project.WithEnvironment(ProtocolEnvironmentName, target.Protocol);
    }
  }

  private static string[] ProjectNames()
  {
    List<string> names = [];
#if api
    names.Add(ApiServerProjectResourceName);
#endif
#if grpc
    names.Add(GrpcServerProjectResourceName);
#endif
#if web
    names.Add(WebServerProjectResourceName);
#endif
#if yarp
    names.Add(YarpResourceName);
#endif
    return [.. names];
  }

  private static bool MatchesDashboard(IDistributedApplicationBuilder builder, string endpoint)
  {
    foreach (string key in DashboardKeys)
    {
      if (SameEndpoint(endpoint, builder.Configuration[key]) || SameEndpoint(endpoint, Environment.GetEnvironmentVariable(key)))
      {
        return true;
      }
    }

    return false;
  }

  private static bool SameEndpoint(string endpoint, string? dashboard)
  {
    if (string.IsNullOrWhiteSpace(dashboard))
    {
      return false;
    }

    return string.Equals(Normalize(endpoint), Normalize(dashboard), StringComparison.Ordinal);
  }

  private static string Normalize(string value)
  {
    string trimmed = value.Trim();
    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)
      || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
      || string.IsNullOrEmpty(uri.Host))
    {
      return trimmed.TrimEnd('/').ToLowerInvariant();
    }

    string path = uri.AbsolutePath.TrimEnd('/');
    return $"{uri.Scheme}://{uri.IdnHost}:{uri.Port}{path}".ToLowerInvariant();
  }

  private static bool IsAbsoluteHttp(string candidate) =>
    Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri)
      && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
      && !string.IsNullOrEmpty(uri.Host);

  private static string? RequiredProtocol(string? value, string key)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    string candidate = value.Trim();
    if (candidate.Equals("grpc", StringComparison.OrdinalIgnoreCase))
    {
      return "grpc";
    }

    if (candidate.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase))
    {
      return "http/protobuf";
    }

    throw new InvalidOperationException($"{key} '{candidate}' must be grpc or http/protobuf.");
  }

  private static string? BlankToNull(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private readonly record struct SecondaryOtlpTarget(string Endpoint, string? Headers, string? Protocol);
}
