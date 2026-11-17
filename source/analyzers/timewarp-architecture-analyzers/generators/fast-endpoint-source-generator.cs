#region Purpose
// Generates FastEndpoint classes from [ApiEndpoint] contract types in referenced assemblies.
#endregion

#region Design
// Opt-in via the EnableApiEndpointGeneration MSBuild property (default false) so any project can
// reference the analyzers package without silently emitting endpoints.
// Scans referenced-assembly symbols rather than the current compilation's syntax: contracts live
// in a separate project from the server that hosts the generated endpoints.
// csc does not attach XML documentation to those references, so GetDocumentationCommentXml is
// empty on a command-line build. The host passes each allow-listed contract assembly's .xml as
// an AdditionalFile, and that file supplies summary and remarks when the symbol comment is empty.
// Host discovery walks only refs stamped [assembly: ApiEndpointsEmbedded] — MSBuild passes the
// transitive closure, so without the marker every framework namespace tree would be walked.
// Product contracts apply the public attributes-package marker; this generator also stamps an
// internal copy when the current compilation declares [ApiEndpoint] (test harness).
// ApiEndpointContractAssemblies (semicolon-separated AssemblyName values, e.g. web-contracts) is
// required when generation is enabled: it is an allow-list, not a docs namespace. Empty, typo, or
// unmarked names report TWE008 (Error) and emit nothing — TWA0019-style fail-closed.
// Equatable FastEndpointGenerationOptions + content-equal EndpointEmitModel so .Collect() does not rebuild
// every *Endpoint.g.cs on unrelated host edits.
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
// The anonymous reason is copied into EndpointSummary.Description so OpenAPI states the
// public choice instead of looking like a forgotten 401. Authorized endpoints keep the
// 401/403 metadata FastEndpoints adds when the verb is not anonymous.
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
// Layout: this class wires the pipeline and runs the per-batch diagnostics (ProcessBatch).
// FastEndpointCollector discovers models, FastEndpointGenerationOptions reads MSBuild options,
// and FastEndpointEmitter renders all source text.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;
using TimeWarp.Architecture.Analyzers.Models;

/// <summary>Source generator that emits FastEndpoint HTTP shims for [ApiEndpoint] contracts.</summary>
[Generator]
public class FastEndpointSourceGenerator : IIncrementalGenerator
{
  // Generator is disabled by default.
  // Consumers must explicitly set <EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>
  // (or via their feature flag system) in their .csproj / Directory.Build.props.

  /// <summary>Registers incremental generator steps for FastEndpoint emission.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<bool> stampMarker =
      context.CompilationProvider.Select(
        static (compilation, _) => HostedRouteDiscovery.CompilationDeclaresApiEndpoint(compilation));

    context.RegisterSourceOutput(stampMarker, static (spc, shouldStamp) =>
    {
      if (!shouldStamp)
      {
        return;
      }

      // Attribute type + assembly stamp together (unlike TypedId post-init of the attribute
      // class on every compilation). Servers that only consume marked refs must not grow an
      // extra generated file on every host edit.
      spc.AddSource("ApiEndpointsEmbeddedAttribute.g.cs", SourceText.From(FastEndpointEmitter.MarkerAttributeSource, Encoding.UTF8));
      spc.AddSource("ApiEndpointsEmbedded.g.cs", SourceText.From(FastEndpointEmitter.MarkerSource, Encoding.UTF8));
    });

    // MSBuild properties controlling generation. Default Enable=false (opt-in).
    // ApiEndpointContractAssemblies is required when enabled (AssemblyName allow-list).
    IncrementalValueProvider<FastEndpointGenerationOptions> generationOptions =
      context.AnalyzerConfigOptionsProvider.Select(
        static (options, _) => FastEndpointGenerationOptions.Read(options.GlobalOptions));

    IncrementalValueProvider<ImmutableArray<string>> missingContractAssemblies =
      context.CompilationProvider
        .Combine(generationOptions)
        .Select(static (tuple, _) => FastEndpointCollector.FindMissingContractAssemblies(tuple.Left, tuple.Right));

    context.RegisterSourceOutput(missingContractAssemblies, static (spc, missing) =>
    {
      foreach (string detail in missing)
      {
        spc.ReportDiagnostic(Diagnostic.Create(
          DiagnosticDescriptors.ApiEndpointContractAssembliesInvalid,
          Location.None,
          detail));
      }
    });

    // Discover equatable emit models (no INamedTypeSymbol in the collected model).
    IncrementalValuesProvider<EndpointEmitModel> candidates =
      context.CompilationProvider
        .Combine(generationOptions)
        .Combine(FastEndpointCollector.ContractDocumentationProvider(context))
        .SelectMany(
          static (tuple, _) =>
          {
            ((Compilation compilation, FastEndpointGenerationOptions genOptions), ContractDocumentation documentation) = tuple;
            if (!genOptions.Enabled || genOptions.ContractAssemblies.IsDefaultOrEmpty)
            {
              return ImmutableArray<EndpointEmitModel>.Empty;
            }

            return FastEndpointCollector.DiscoverCandidates(compilation, genOptions.ContractAssemblies, documentation);
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

    IncrementalValueProvider<(ImmutableArray<EndpointEmitModel> Models, bool FeAvailable, FastEndpointGenerationOptions Options)> input =
      batch.Combine(fastEndpointsAvailable).Combine(generationOptions)
        .Select(static (tuple, _) =>
        {
          ((ImmutableArray<EndpointEmitModel> models, bool feAvailable), FastEndpointGenerationOptions options) = tuple;
          return (models, feAvailable, options);
        });

    context.RegisterSourceOutput(input, static (spc, data) => ProcessBatch(spc, data.Models, data.FeAvailable, data.Options));
  }

  private static void ProcessBatch(
    SourceProductionContext spc,
    ImmutableArray<EndpointEmitModel> models,
    bool feAvailable,
    FastEndpointGenerationOptions options)
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
        string endpointClass = FastEndpointEmitter.GenerateEndpointClass(model);
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
