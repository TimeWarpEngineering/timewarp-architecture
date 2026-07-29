#region Purpose
// Generates FastEndpoint classes from [ApiEndpoint] contract types in referenced assemblies.
#endregion

#region Design
// Opt-in via the EnableApiEndpointGeneration MSBuild property (default false) so any project can
// reference the analyzers package without silently emitting endpoints.
// Scans referenced-assembly symbols rather than the current compilation's syntax: contracts live
// in a separate project from the server that hosts the generated endpoints.
// Optional ApiEndpointContractAssemblies (semicolon-separated assembly names) restricts which
// referenced assemblies are scanned — needed when the host transitively references other contract
// assemblies (e.g. web-server → web-spa → api-contracts) and must not emit foreign endpoints.
// Empty/unset = scan all referenced assemblies (api-server default).
// Reports SG002 once per batch when FastEndpoints/BaseFastEndpoint are absent — feature flags
// can strip those references while the generator package remains attached.
// Catches all exceptions (CA1031): a throwing generator would break the entire compilation.
// Request type is Query or Command per metadata.RequestTypeName; HTTP verb comes from resolved
// enum member name (not the underlying int). Fail-closed verbs (F-008 / TWE007): never default
// an unknown verb to Get — report and skip that contract.
// Missing nested Query/Command → TWE002, no emission.
// Auth (task 110, fail-closed default):
// [EndpointAuthorize] → Policies/Roles/AuthSchemes; [EndpointAllowAnonymous] → AllowAnonymous();
// NEITHER attribute → emit nothing (FastEndpoints requires authentication by default).
// Empty request DTOs (no public properties) get EmptyRequestBinder — FE's default binder rejects them.
// Summary/Description only — no weather-specific ExampleRequest.
// OpenAPI feature grouping: FE Tags() is endpoint-filter metadata only (no relationship with
// OpenAPI tags — see FastEndpoints configuration docs). Scalar sidebar groups by OpenAPI
// operation tags, so emission pairs Tags(...) with Description(d => d.WithTags(...)).
// Leaf feature Id comes from EndpointEmitModel (…Features.Admin.Roles → "Roles").
// F-003: per-compilation route conflict via equatable models + .Collect() + in-batch group-by
// (Route, HttpVerb). TWE003 on ALL parties; generate NONE of a conflict group. No static
// ConcurrentDictionary / RouteRegistry — IDE incremental and multi-project compiler-server
// runs cannot self-conflict or cross-pollute.
// F-004: discovery via HostedRouteDiscovery (linked shared source); [ClientOnlyContract] on
// outer or nested skips generation (TWA0020 flags the contradiction in convention analyzers).
// F-005: base type is always BaseFastEndpoint (EndpointType override deleted).
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;
using TimeWarp.Architecture.Analyzers.Models;

[Generator]
public class FastEndpointSourceGenerator : IIncrementalGenerator
{
  // Generator is disabled by default.
  // Consumers must explicitly set <EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>
  // (or via their feature flag system) in their .csproj / Directory.Build.props.

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // MSBuild properties controlling generation. Default Enable=false (opt-in). Optional
    // ApiEndpointContractAssemblies restricts which referenced assemblies contribute contracts.
    IncrementalValueProvider<GenerationOptions> generationOptions =
      context.AnalyzerConfigOptionsProvider.Select(
        static (options, _) => GenerationOptions.Read(options.GlobalOptions));

    // Discover equatable emit models (no INamedTypeSymbol in the collected model).
    IncrementalValuesProvider<EndpointEmitModel> candidates =
      context.CompilationProvider
        .Combine(generationOptions)
        .SelectMany(
          static (tuple, _) =>
          {
            (Compilation compilation, GenerationOptions genOptions) = tuple;
            if (!genOptions.Enabled)
            {
              return ImmutableArray<EndpointEmitModel>.Empty;
            }

            return DiscoverCandidates(compilation, genOptions.ContractAssemblies);
          });

    IncrementalValueProvider<ImmutableArray<EndpointEmitModel>> batch = candidates.Collect();

    IncrementalValueProvider<bool> fastEndpointsAvailable =
      context.CompilationProvider.Select(
        static (compilation, _) =>
        {
          INamedTypeSymbol? fastEndpointsSymbol =
            compilation.GetTypeByMetadataName("FastEndpoints.IEndpoint");
          INamedTypeSymbol? baseFastEndpointSymbol =
            compilation.GetTypeByMetadataName("TimeWarp.Foundation.Features.BaseFastEndpoint`2");
          return fastEndpointsSymbol is not null && baseFastEndpointSymbol is not null;
        });

    IncrementalValueProvider<(ImmutableArray<EndpointEmitModel> Models, bool FeAvailable, GenerationOptions Options)> input =
      batch.Combine(fastEndpointsAvailable).Combine(generationOptions)
        .Select(static (tuple, _) =>
        {
          ((ImmutableArray<EndpointEmitModel> models, bool feAvailable), GenerationOptions options) = tuple;
          return (models, feAvailable, options);
        });

    context.RegisterSourceOutput(input, static (spc, data) => ProcessBatch(spc, data.Models, data.FeAvailable, data.Options));
  }

  private static ImmutableArray<EndpointEmitModel> DiscoverCandidates(
    Compilation compilation,
    HashSet<string>? contractAssemblies)
  {
    INamedTypeSymbol? apiEndpointAttributeSymbol =
      compilation.GetTypeByMetadataName(HostedRouteDiscovery.ApiEndpointAttributeFullName);
    if (apiEndpointAttributeSymbol is null)
    {
      return ImmutableArray<EndpointEmitModel>.Empty;
    }

    IEnumerable<IAssemblySymbol> assemblies = compilation.SourceModule.ReferencedAssemblySymbols;
    if (contractAssemblies is { Count: > 0 } allowed)
    {
      assemblies = assemblies.Where(assembly => allowed.Contains(assembly.Name));
    }

    ImmutableArray<EndpointEmitModel>.Builder builder = ImmutableArray.CreateBuilder<EndpointEmitModel>();

    foreach (IAssemblySymbol assembly in assemblies)
    {
      foreach (INamespaceSymbol ns in HostedRouteDiscovery.GetAllNamespaces(assembly.GlobalNamespace))
      {
        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
          bool hasApiEndpoint = type.GetAttributes().Any(attr =>
            SymbolEqualityComparer.Default.Equals(attr.AttributeClass, apiEndpointAttributeSymbol)
            || attr.AttributeClass?.Name == HostedRouteDiscovery.ApiEndpointAttributeSimpleName);
          if (!hasApiEndpoint)
          {
            continue;
          }

          INamedTypeSymbol? requestClass = type.GetTypeMembers()
            .FirstOrDefault(static m => m.Name is "Query" or "Command");

          // ClientOnly on outer or nested → not hosted (TWA0020 warns separately).
          if (HostedRouteDiscovery.HasClientOnlyOnOperationOrRequest(type, requestClass))
          {
            continue;
          }

          builder.Add(EndpointEmitModel.FromSymbol(type));
        }
      }
    }

    return builder.ToImmutable();
  }

  private static void ProcessBatch(
    SourceProductionContext spc,
    ImmutableArray<EndpointEmitModel> models,
    bool feAvailable,
    GenerationOptions options)
  {
    if (!options.Enabled)
    {
      return;
    }

    if (!feAvailable)
    {
      spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingFastEndpoints, Location.None));
      return;
    }

    try
    {
      // Phase 1: per-contract shape diagnostics (TWE002 / TWE007); only valid models emit.
      var emitCandidates = new List<EndpointEmitModel>();

      foreach (EndpointEmitModel model in models)
      {
        if (model.MissingQueryOrCommand)
        {
          spc.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ApiEndpointMissingQuery,
            Location.None,
            model.ClassName));
          continue;
        }

        if (model.VerbUnresolved || string.IsNullOrEmpty(model.HttpVerb) || string.IsNullOrWhiteSpace(model.Route))
        {
          spc.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ApiEndpointUnknownHttpVerb,
            Location.None,
            model.ClassName,
            string.IsNullOrEmpty(model.UnresolvedVerbDisplay)
              ? (string.IsNullOrWhiteSpace(model.Route) ? "<empty route>" : "<missing>")
              : model.UnresolvedVerbDisplay));
          continue;
        }

        emitCandidates.Add(model);
      }

      // Phase 2: route+verb conflict groups — TWE003 on ALL parties; generate NONE of the group.
      var groups = emitCandidates
        .GroupBy(static m => (m.Route, m.HttpVerb), RouteVerbComparer.Instance)
        .ToList();

      foreach (IGrouping<(string Route, string HttpVerb), EndpointEmitModel> group in groups)
      {
        var parties = group.ToList();
        if (parties.Count > 1)
        {
          string allNames = string.Join(", ", parties.Select(static p => p.ClassName).OrderBy(static n => n, StringComparer.Ordinal));
          foreach (EndpointEmitModel party in parties)
          {
            spc.ReportDiagnostic(Diagnostic.Create(
              DiagnosticDescriptors.ApiEndpointRouteConflict,
              Location.None,
              party.Route,
              party.HttpVerb,
              allNames));
          }

          continue;
        }

        EndpointEmitModel model = parties[0];
        string endpointClass = GenerateEndpointClass(model);
        string fileName = $"{model.ClassName}Endpoint.g.cs";
        spc.AddSource(fileName, SourceText.From(endpointClass, Encoding.UTF8));
      }
    }
    catch (Exception ex) // CA1031: Source generators must be resilient
    {
      spc.ReportDiagnostic(Diagnostic.Create(
        DiagnosticDescriptors.SourceGeneratorLog,
        Location.None,
        $"Error generating endpoints: {ex.Message}"));
    }
  }

  private static string GenerateEndpointClass(EndpointEmitModel metadata)
  {
    // FE Tags() = endpoint-filter metadata only. OpenAPI/Scalar need Description.WithTags.
    string tagArgs = metadata.Tags.Length > 0
      ? string.Join(", ", metadata.Tags.Select(static t => $"\"{t}\""))
      : string.Empty;

    string tagsFilter = metadata.Tags.Length > 0
      ? $"Tags({tagArgs});"
      : "";

    string auth = BuildAuthConfiguration(metadata);

    // FE default RequestBinder TypeInit-fails on DTOs with zero public properties.
    string emptyBinder = metadata.IsEmptyRequest
      ? $"RequestBinder(new EmptyRequestBinder<{metadata.ClassName}.{metadata.RequestTypeName}>());"
      : "";

    // Fold OpenAPI WithTags into Description when Produces is also emitted; otherwise emit tags alone.
    string withTagsChain = metadata.Tags.Length > 0
      ? $".WithTags({tagArgs})"
      : "";

    string summary = !string.IsNullOrEmpty(metadata.Summary)
      ? $$"""
            Summary(s =>
            {
              s.Summary = "{{EscapeForStringLiteral(metadata.Summary)}}";
              s.Description = "{{EscapeForStringLiteral(metadata.Description)}}";
            });

            Description(d => d{{withTagsChain}}.Produces<{{metadata.ClassName}}.Response>(200, "Success").ProducesProblem(400, "Bad Request")
            );
          """
      : metadata.Tags.Length > 0
        ? $$"""
            Description(d => d.WithTags({{tagArgs}}));
          """
        : "";

    string requestType = metadata.RequestTypeName;

    return $$"""
             using FastEndpoints;
             using OneOf;
             using System.Threading;
             using System.Threading.Tasks;
             using TimeWarp.Foundation.Features;

             namespace {{metadata.Namespace}};

             /// <summary>
             /// {{metadata.Summary}}
             /// </summary>
             /// <remarks>
             /// {{metadata.Description}}
             /// </remarks>
             public class {{metadata.ClassName}}Endpoint : BaseFastEndpoint<{{metadata.ClassName}}.{{requestType}}, {{metadata.ClassName}}.Response>
             {
               public override void Configure()
               {
                 {{metadata.HttpVerb}}("{{metadata.Route}}");
                 {{auth}}
                 {{emptyBinder}}
                 {{tagsFilter}}
                 {{summary}}
               }
             }
             """;
  }

  /// <summary>
  /// Builds FastEndpoints Configure() auth lines from [EndpointAuthorize]/[EndpointAllowAnonymous]
  /// metadata. Task 110 fail-closed default: metadata.AllowAnonymous is true ONLY when
  /// [EndpointAllowAnonymous] was present (see EndpointEmitModel.FromSymbol) — in that case emit
  /// AllowAnonymous(). Otherwise: Policy → Policies(...); Roles → Roles(...);
  /// Schemes → AuthSchemes(...); and if NONE of those were set this method emits NOTHING —
  /// FE requires auth by default when AllowAnonymous() is never called.
  /// </summary>
  private static string BuildAuthConfiguration(EndpointEmitModel metadata)
  {
    if (metadata.AllowAnonymous)
    {
      return "AllowAnonymous();";
    }

    var lines = new List<string>();

    if (!string.IsNullOrEmpty(metadata.AuthenticationSchemes))
    {
      lines.Add($"AuthSchemes({FormatCsvStringArgs(metadata.AuthenticationSchemes)});");
    }

    if (!string.IsNullOrEmpty(metadata.AuthorizationPolicy))
    {
      lines.Add($"Policies(\"{EscapeForStringLiteral(metadata.AuthorizationPolicy)}\");");
    }
    else if (!string.IsNullOrEmpty(metadata.Roles))
    {
      lines.Add($"Roles({FormatCsvStringArgs(metadata.Roles)});");
    }

    return lines.Count > 0
      ? string.Join("\n         ", lines)
      : string.Empty;
  }

  private static string FormatCsvStringArgs(string csv)
  {
    IEnumerable<string> parts = csv
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Select(part => $"\"{EscapeForStringLiteral(part)}\"");
    return string.Join(", ", parts);
  }

  private static string EscapeForStringLiteral(string value)
    => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

  private sealed class GenerationOptions
  {
    public bool Enabled { get; }
    public HashSet<string>? ContractAssemblies { get; }

    private GenerationOptions(bool enabled, HashSet<string>? contractAssemblies)
    {
      Enabled = enabled;
      ContractAssemblies = contractAssemblies;
    }

    public static GenerationOptions Read(AnalyzerConfigOptions globalOptions)
    {
      bool enabled = false;
      if (globalOptions.TryGetValue("build_property.EnableApiEndpointGeneration", out string? enableValue) &&
          bool.TryParse(enableValue, out bool parsed))
      {
        enabled = parsed;
      }

      HashSet<string>? contractAssemblies = null;
      if (globalOptions.TryGetValue("build_property.ApiEndpointContractAssemblies", out string? assembliesValue) &&
          !string.IsNullOrWhiteSpace(assembliesValue))
      {
        contractAssemblies = new HashSet<string>(
          assembliesValue.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
          StringComparer.OrdinalIgnoreCase);
      }

      return new GenerationOptions(enabled, contractAssemblies);
    }
  }

  /// <summary>Ordinal route + verb grouping (case-sensitive path match as declared on the contract).</summary>
  private sealed class RouteVerbComparer : IEqualityComparer<(string Route, string HttpVerb)>
  {
    public static readonly RouteVerbComparer Instance = new();

    public bool Equals((string Route, string HttpVerb) x, (string Route, string HttpVerb) y)
      => string.Equals(x.Route, y.Route, StringComparison.Ordinal)
         && string.Equals(x.HttpVerb, y.HttpVerb, StringComparison.Ordinal);

    public int GetHashCode((string Route, string HttpVerb) obj)
      => HashCode.Combine(
        StringComparer.Ordinal.GetHashCode(obj.Route),
        StringComparer.Ordinal.GetHashCode(obj.HttpVerb));
  }
}
