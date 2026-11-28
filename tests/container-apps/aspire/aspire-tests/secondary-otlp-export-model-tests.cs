#region Purpose
// AppHost model inspection (task 536-001): run mode forwards a secondary OTLP collector onto the
// declared project resources, and publish mode does not.
#endregion

#region Design
// Model-only, like signoz-link-model-tests: DistributedApplicationTestingBuilder.CreateAsync runs the
// AppHost entry point up to Build, so nothing starts. Command-line configuration overrides user
// secrets, so each fact passes the SigNoz OTLP keys explicitly (an empty value is the unset case).
// The inherited endpoint is the AppHost process environment. These facts set and restore that
// environment through SecondaryOtlpEnvironment, the gate shared with the ServiceDefaults suite.
// Forward reads it while the model is built and stores literal
// strings on the resource callbacks. A missing project resource is skipped (a template flag can omit
// it). The fact fails when none of the declared project resources exist. Publish facts pass an
// endpoint and still expect no Observability__SecondaryOtlp key. The generated-manifest half lives
// in the compose, kubernetes, aca, and aca-neon suites.
#endregion

namespace Aspire.Tests;

[TestTag("Integration")]
public class SecondaryOtlpExportModel_Given_
{
  private const string EndpointKey = "Observability__SecondaryOtlp__Endpoint";
  private const string HeadersKey = "Observability__SecondaryOtlp__Headers";
  private const string ProtocolKey = "Observability__SecondaryOtlp__Protocol";
  private const string PrimaryEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
  private const string ExplicitEndpoint = "https://otlp.example:4318";
  private const string InheritedEndpoint = "https://inherited.example:4318";
  private const string ExplicitHeaders = "signoz-access-token=explicit-secret";
  private const string InheritedHeaders = "signoz-access-token=inherited-secret";

  private static readonly string[] ProjectNames = ["api-server", "grpc-server", "web-server", "ingress"];
  private static readonly string[] OtherNames = ["postgres", "web-migrations", "timewarp-devtools"];
  private static readonly Dictionary<string, string?> NoEnvironment = [];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SecondaryOtlpExportModel_Given_>();

  public static async Task RunMode_Should_ForwardExplicitEndpointHeadersAndProtocol()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      ExplicitEndpoint,
      ExplicitHeaders,
      "HTTP/Protobuf",
      disabled: null,
      NoEnvironment);

    await AssertForwardedAsync(appHost, ExplicitEndpoint, ExplicitHeaders, "http/protobuf");
  }

  public static async Task RunMode_Should_ForwardAbsoluteHttpEndpoint()
  {
    const string endpoint = "http://otlp.example:4318";
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      endpoint,
      headers: null,
      protocol: null,
      disabled: null,
      NoEnvironment);

    await AssertForwardedAsync(appHost, endpoint, headers: null, protocol: null);
  }

  public static async Task RunMode_Should_FallBackToInheritedEndpoint()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      endpoint: null,
      headers: null,
      protocol: null,
      disabled: null,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = InheritedHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      });

    await AssertForwardedAsync(appHost, InheritedEndpoint, InheritedHeaders, "grpc");
  }

  public static async Task RunMode_Should_PreferExplicitEndpointOverInherited()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      ExplicitEndpoint,
      ExplicitHeaders,
      "http/protobuf",
      disabled: null,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = InheritedHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      });

    await AssertForwardedAsync(appHost, ExplicitEndpoint, ExplicitHeaders, "http/protobuf");
  }

  [Input("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL")]
  [Input("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL")]
  public static async Task RunMode_Should_SkipInheritedEndpointThatMatchesDashboardUrl(string dashboardVariable)
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      endpoint: null,
      headers: null,
      protocol: null,
      disabled: null,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = InheritedHeaders,
        [dashboardVariable] = InheritedEndpoint,
      });

    await AssertAbsentAsync(appHost);
  }

  public static async Task RunMode_Should_SkipInheritedEndpointThatMatchesDashboardUrlIgnoringCaseAndSlash()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      endpoint: null,
      headers: null,
      protocol: null,
      disabled: null,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "HTTP://OTLP.Example:4318/",
      },
      extraArguments: ["--DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://otlp.example:4318"]);

    await AssertAbsentAsync(appHost);
  }

  public static async Task RunMode_Should_SkipFallbackWhenDisabled()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      endpoint: null,
      headers: null,
      protocol: null,
      disabled: "TRUE",
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
      });

    await AssertAbsentAsync(appHost);
  }

  public static async Task RunMode_Should_ForwardExplicitEndpointWhenDisabled()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      ExplicitEndpoint,
      headers: null,
      protocol: "grpc",
      disabled: "true",
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = InheritedHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
      });

    await AssertForwardedAsync(appHost, ExplicitEndpoint, headers: null, protocol: "grpc");
  }

  public static async Task RunMode_Should_TreatBlankExplicitEndpointAsUnset()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      "   ",
      headers: null,
      protocol: null,
      disabled: null,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
      });

    await AssertForwardedAsync(appHost, InheritedEndpoint, headers: null, protocol: null);
  }

  [Input("http://")]
  [Input("otlp.example:4318")]
  [Input("ftp://otlp.example/v1/traces")]
  public static async Task InvalidEndpoint_Should_FailStartup(string candidate)
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
      async () => await CreateRunModeAsync(candidate, headers: null, protocol: null, disabled: null, NoEnvironment));

    exception.Message.ShouldContain("Observability:SigNoz:OtlpEndpoint");
  }

  public static async Task InvalidExplicitProtocol_Should_FailStartup()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
      async () => await CreateRunModeAsync(ExplicitEndpoint, headers: null, protocol: "amqp", disabled: null, NoEnvironment));

    exception.Message.ShouldContain("Observability:SigNoz:OtlpProtocol");
  }

  public static async Task InvalidInheritedProtocol_Should_FailStartup()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
      async () => await CreateRunModeAsync(
        endpoint: null,
        headers: null,
        protocol: null,
        disabled: null,
        new Dictionary<string, string?>
        {
          ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
          ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "amqp",
        }));

    exception.Message.ShouldContain("OTEL_EXPORTER_OTLP_PROTOCOL");
  }

  [Input("not-a-url")]
  [Input("ftp://files.example/otlp")]
  public static async Task RunMode_Should_IgnoreInvalidInheritedEndpoint(string inherited)
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(
      endpoint: null,
      headers: null,
      protocol: null,
      disabled: null,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = inherited,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "amqp",
      });

    await AssertAbsentAsync(appHost);
  }

  public static async Task PublishMode_Should_OmitSecondaryOtlp()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreatePublishAsync(
      ExplicitEndpoint,
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = InheritedEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = InheritedHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      });

    await AssertAbsentAsync(appHost, DistributedApplicationOperation.Publish);
    appHost.Resources.ShouldNotContain(resource => resource.Name == "signoz");
  }

  private static async Task AssertForwardedAsync(
    IDistributedApplicationTestingBuilder appHost,
    string endpoint,
    string? headers,
    string? protocol)
  {
    List<string> found = [];
    foreach (string name in ProjectNames)
    {
      IResource? resource = appHost.Resources.SingleOrDefault(candidate => candidate.Name == name);
      if (resource is null)
      {
        continue;
      }

      found.Add(name);
      Dictionary<string, string> environment = await EnvironmentOfAsync(resource, DistributedApplicationOperation.Run);
      environment[EndpointKey].ShouldBe(endpoint);
      if (headers is null)
      {
        environment.ContainsKey(HeadersKey).ShouldBeFalse();
      }
      else
      {
        environment[HeadersKey].ShouldBe(headers);
      }

      if (protocol is null)
      {
        environment.ContainsKey(ProtocolKey).ShouldBeFalse();
      }
      else
      {
        environment[ProtocolKey].ShouldBe(protocol);
      }

      environment.TryGetValue(PrimaryEndpointKey, out string? primary).ShouldBeTrue($"{name} lost the dashboard OTLP endpoint");
      primary.ShouldNotBe(endpoint);
    }

    found.ShouldNotBeEmpty("no declared project resource exists to receive the secondary OTLP endpoint");
    await AssertOtherResourcesOmitAsync(appHost, DistributedApplicationOperation.Run);
  }

  private static async Task AssertAbsentAsync(
    IDistributedApplicationTestingBuilder appHost,
    DistributedApplicationOperation operation = DistributedApplicationOperation.Run)
  {
    List<string> found = [];
    foreach (string name in ProjectNames)
    {
      IResource? resource = appHost.Resources.SingleOrDefault(candidate => candidate.Name == name);
      if (resource is null)
      {
        continue;
      }

      found.Add(name);
      Dictionary<string, string> environment = await EnvironmentOfAsync(resource, operation);
      environment.Keys.ShouldNotContain(
        key => key.Contains("SecondaryOtlp", StringComparison.Ordinal),
        $"{name} forwards a secondary OTLP setting");
    }

    found.ShouldNotBeEmpty("no declared project resource exists");
    await AssertOtherResourcesOmitAsync(appHost, operation);
  }

  private static async Task AssertOtherResourcesOmitAsync(
    IDistributedApplicationTestingBuilder appHost,
    DistributedApplicationOperation operation)
  {
    foreach (string name in OtherNames)
    {
      IResource? resource = appHost.Resources.SingleOrDefault(candidate => candidate.Name == name);
      if (resource is null)
      {
        continue;
      }

      Dictionary<string, string> environment = await EnvironmentOfAsync(resource, operation);
      environment.Keys.ShouldNotContain(
        key => key.Contains("SecondaryOtlp", StringComparison.Ordinal),
        $"{name} forwards a secondary OTLP setting");
    }
  }

  private static async Task<Dictionary<string, string>> EnvironmentOfAsync(
    IResource resource,
    DistributedApplicationOperation operation)
  {
    EnvironmentCallbackContext context = new(new DistributedApplicationExecutionContext(operation), resource);
    foreach (EnvironmentCallbackAnnotation callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
    {
      await callback.Callback(context);
    }

    return context.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString() ?? "");
  }

  private static Task<IDistributedApplicationTestingBuilder> CreateRunModeAsync(
    string? endpoint,
    string? headers,
    string? protocol,
    string? disabled,
    IReadOnlyDictionary<string, string?> environment,
    IReadOnlyList<string>? extraArguments = null)
  {
    List<string> args =
    [
      "--Observability:SigNoz:OtlpEndpoint=" + (endpoint ?? ""),
      "--Observability:SigNoz:OtlpHeaders=" + (headers ?? ""),
      "--Observability:SigNoz:OtlpProtocol=" + (protocol ?? ""),
      "--Observability:SecondaryOtlp:Disabled=" + (disabled ?? ""),
    ];
    if (extraArguments is not null)
    {
      args.AddRange(extraArguments);
    }

    return CreateCoreAsync([.. args], environment, publish: false);
  }

  private static Task<IDistributedApplicationTestingBuilder> CreatePublishAsync(
    string? endpoint,
    IReadOnlyDictionary<string, string?> environment) =>
    CreateCoreAsync(
      [
        "--operation", "publish",
        "--step", "publish-compose",
        "--Observability:SigNoz:OtlpEndpoint=" + (endpoint ?? ""),
        "--Observability:SigNoz:OtlpHeaders=",
        "--Observability:SigNoz:OtlpProtocol=",
        "--Observability:SecondaryOtlp:Disabled=",
      ],
      environment,
      publish: true);

  private static Task<IDistributedApplicationTestingBuilder> CreateCoreAsync(
    string[] args,
    IReadOnlyDictionary<string, string?> environment,
    bool publish) =>
    SecondaryOtlpEnvironment.IsolateAsync(environment, () => CreateBuilderAsync(args, publish));

  private static Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync(string[] args, bool publish)
  {
    if (publish)
    {
      return DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        args,
        (_, settings) => settings.EnvironmentName = "Production");
    }

    return DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(args);
  }
}
