#region Purpose
// Generates the WebServerApiRoutePrefixes list the YARP ingress loops over, from the [ApiRoute]
// templates on hosted web-contracts operations — so the ingress route carve-outs cannot drift from
// the contracts (task 107; the drift class that shipped /api/identity and /api/Roles unreachable).
#endregion

#region Design
// Opt-in via EnableIngressRouteGeneration (default false) so the Generators package can attach to a
// project without emitting this list unless the project is an ingress host.
// Scans referenced-assembly symbols (not the current compilation's syntax): the contracts live in
// web-contracts, a separate project from the AppHost/yarp that host the ingress. Same cross-assembly
// approach as FastEndpointSourceGenerator; participate rule matches EndpointMetadata.FromSymbol
// (outer [ApiEndpoint] + nested Query/Command [ApiRoute], simple-name attribute matching) minus
// [ClientOnlyContract] (those never reach the server, so they must not gain an ingress route).
// IngressWebContractAssemblies (semicolon/comma list) names the assemblies whose routes belong to
// Web.Server (web-contracts). Empty = scan every referenced assembly (kept only for parity with the
// FastEndpoint generator; the ingress hosts always name web-contracts explicitly).
//
// GLOBAL NAMESPACE (deliberate, task 115 lesson): the dotnet-new sourceName rewrite renames the
// template's root namespace per generated app but CANNOT reach generator output — a hardcoded
// namespace here would compile in this repo and break in every generated app whose consumer sits in
// a renamed namespace. A global-namespace type is reachable from any namespace, so the emitted class
// is namespace-agnostic by construction.
//
// ALWAYS emits when enabled, even with an empty result (ImmutableArray<string>.Empty): the AppHost's
// `foreach (var prefix in WebServerApiRoutePrefixes.All)` must compile in every feature-flag combo,
// including ones where no participating contract exists.
//
// Collapse: a route is reduced to its top-level `api/<segment>` prefix so five `api/identity/...`
// operations become one `/api/identity/{**catch-all}` ingress route whose literal segment outranks
// the Api.Server `/api/{**catch-all}`. A route whose first segment is not `api` is skipped — the
// Web.Server catch-all already serves everything non-api. Dedupe OrdinalIgnoreCase, sort Ordinal so
// the emitted list (and therefore the ingress config) is deterministic.
//
// Diagnostics (both Warning; declared in AnalyzerReleases.Unshipped.md):
//   TWA0017 — a generated web prefix would shadow another server's route space: it equals or is a
//             parent of a hosted route in another *contracts* assembly (the Api.Server set), or a
//             web route's first segment collides with an IngressReservedPathPrefixes entry (grpc).
//   TWA0018 — a route cannot be collapsed to a top-level prefix (bare `api`, or a parameterized
//             second segment like `api/{id}`) — the ingress cannot own an ambiguous prefix.
//   TWA0019 — a name in IngressWebContractAssemblies matches no referenced assembly (typo,
//             AssemblyName override, template rename): the silent-empty trap this task exists to
//             kill — every carve-out would vanish with no signal. Reported per missing name.
// Location.None: the offending symbol lives in a referenced assembly with no syntax location here.
// Catches all exceptions (CA1031): a throwing generator would break the whole compilation.
#endregion

namespace TimeWarp.Architecture.Analyzers;

[Generator]
public class IngressRoutePrefixGenerator : IIncrementalGenerator
{
  private const string ApiEndpointAttributeFullName = "TimeWarp.Architecture.Attributes.ApiEndpointAttribute";
  private const string ApiRouteAttributeSimpleName = "ApiRouteAttribute";
  private const string ClientOnlyContractAttributeSimpleName = "ClientOnlyContractAttribute";
  private const string ApiSegment = "api";
  private static readonly char[] SlashSeparator = { '/' };
  private static readonly char[] ListSeparators = { ';', ',' };

  private static readonly DiagnosticDescriptor RouteSpaceCollisionDescriptor = new
  (
    "TWA0017",
    "Ingress web prefix shadows another server's route space",
    "Generated ingress prefix '/{0}/{{**catch-all}}' shadows '{1}' owned by another server — the web catch-all would capture requests meant for {2}",
    "Design",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  private static readonly DiagnosticDescriptor NonDerivablePrefixDescriptor = new
  (
    "TWA0018",
    "Ingress route prefix is not derivable",
    "Web-contracts route '{0}' cannot be collapsed to a top-level ingress prefix ({1}) — the ingress cannot own an ambiguous or parameterized top-level segment",
    "Design",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  private static readonly DiagnosticDescriptor MissingContractsAssemblyDescriptor = new
  (
    "TWA0019",
    "Configured ingress contracts assembly not found",
    "Configured ingress contracts assembly '{0}' was not found among the compilation's referenced assemblies — no Web.Server ingress routes will be generated from it; check IngressWebContractAssemblies for a typo, or a renamed/stale assembly name",
    "Design",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<GenerationOptions> options = context.AnalyzerConfigOptionsProvider.Select(
      static (provider, _) => GenerationOptions.Read(provider.GlobalOptions));

    IncrementalValueProvider<(Compilation Compilation, GenerationOptions Options)> input =
      context.CompilationProvider.Combine(options);

    context.RegisterSourceOutput(input, static (spc, tuple) => Execute(spc, tuple.Compilation, tuple.Options));
  }

  private static void Execute(SourceProductionContext spc, Compilation compilation, GenerationOptions options)
  {
    if (!options.Enabled)
    {
      return;
    }

    try
    {
      // TWA0019: a configured contracts-assembly name that matches no reference is a silent-empty
      // trap — every ingress carve-out would vanish with no signal (typo, AssemblyName override, or
      // a template rename). Report per missing name; generation still emits an (empty) All.
      if (options.SourceAssemblies.Count > 0)
      {
        var referencedNames = compilation.SourceModule.ReferencedAssemblySymbols
          .Select(referenced => referenced.Name)
          .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string configured in options.SourceAssemblies)
        {
          if (!referencedNames.Contains(configured))
          {
            spc.ReportDiagnostic(Diagnostic.Create(MissingContractsAssemblyDescriptor, Location.None, configured));
          }
        }
      }

      INamedTypeSymbol? apiEndpointAttribute = compilation.GetTypeByMetadataName(ApiEndpointAttributeFullName);

      // Hosted web routes (source assemblies) and the collision set (other contracts assemblies).
      List<HostedRoute> webRoutes = new();
      List<HostedRoute> foreignRoutes = new();

      if (apiEndpointAttribute is not null)
      {
        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
          bool isSource = options.SourceAssemblies.Count == 0
            ? true
            : options.SourceAssemblies.Contains(assembly.Name);

          bool isForeignContracts = !isSource
            && assembly.Name.Contains("contracts", StringComparison.OrdinalIgnoreCase);

          if (!isSource && !isForeignContracts)
          {
            continue;
          }

          foreach (HostedRoute route in EnumerateHostedRoutes(assembly, apiEndpointAttribute))
          {
            (isSource ? webRoutes : foreignRoutes).Add(route);
          }
        }
      }

      // Collapse the web routes to top-level api/<segment> prefixes; TWA0018 on the non-derivable ones.
      SortedSet<string> prefixes = new(StringComparer.Ordinal);
      HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

      foreach (HostedRoute route in webRoutes)
      {
        string[] segments = route.Template.Trim('/').Split(SlashSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 || !segments[0].Equals(ApiSegment, StringComparison.OrdinalIgnoreCase))
        {
          // Non-api (or empty) routes fall to the Web.Server catch-all; not an ingress carve-out.
          continue;
        }

        if (segments.Length == 1)
        {
          spc.ReportDiagnostic(Diagnostic.Create(NonDerivablePrefixDescriptor, Location.None, route.Template, "bare 'api' with no top-level segment"));
          continue;
        }

        if (segments[1].Contains('{', StringComparison.Ordinal))
        {
          spc.ReportDiagnostic(Diagnostic.Create(NonDerivablePrefixDescriptor, Location.None, route.Template, "parameterized second segment"));
          continue;
        }

        string prefix = ApiSegment + "/" + segments[1];
        if (seen.Add(prefix))
        {
          prefixes.Add(prefix);
        }
      }

      ReportCollisions(spc, prefixes, foreignRoutes, webRoutes, options.ReservedPrefixes);

      spc.AddSource("WebServerApiRoutePrefixes.g.cs", SourceText.From(BuildSource(prefixes), Encoding.UTF8));
    }
    catch (Exception ex) // CA1031: a source generator must not throw into the compilation.
    {
      var logDescriptor = new DiagnosticDescriptor("SG001", "Source Generator Log", "{0}", "SourceGenerator", DiagnosticSeverity.Warning, true);
      spc.ReportDiagnostic(Diagnostic.Create(logDescriptor, Location.None, $"IngressRoutePrefixGenerator error: {ex.Message}"));
    }
  }

  private static void ReportCollisions
  (
    SourceProductionContext spc,
    SortedSet<string> prefixes,
    List<HostedRoute> foreignRoutes,
    List<HostedRoute> webRoutes,
    ImmutableArray<string> reservedPrefixes
  )
  {
    // (a) a web prefix P shadows a foreign hosted route T when T == P or T is under P (P + "/").
    foreach (string prefix in prefixes)
    {
      string prefixSlash = prefix + "/";
      foreach (HostedRoute foreign in foreignRoutes)
      {
        string template = foreign.Template.Trim('/');
        if (template.Equals(prefix, StringComparison.OrdinalIgnoreCase)
          || template.StartsWith(prefixSlash, StringComparison.OrdinalIgnoreCase))
        {
          spc.ReportDiagnostic(Diagnostic.Create(RouteSpaceCollisionDescriptor, Location.None, prefix, foreign.Template, foreign.AssemblyName));
        }
      }
    }

    // (b) a web route whose first segment equals a reserved prefix (grpc) would be captured by the
    // ingress ahead of the reserved server's own route.
    if (reservedPrefixes.Length > 0)
    {
      foreach (HostedRoute route in webRoutes)
      {
        string[] segments = route.Template.Trim('/').Split(SlashSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
          continue;
        }

        foreach (string reserved in reservedPrefixes)
        {
          if (segments[0].Equals(reserved, StringComparison.OrdinalIgnoreCase))
          {
            spc.ReportDiagnostic(Diagnostic.Create(RouteSpaceCollisionDescriptor, Location.None, segments[0], route.Template, $"the reserved '{reserved}' prefix"));
          }
        }
      }
    }
  }

  private static IEnumerable<HostedRoute> EnumerateHostedRoutes(IAssemblySymbol assembly, INamedTypeSymbol apiEndpointAttribute)
  {
    foreach (INamespaceSymbol ns in GetAllNamespaces(assembly.GlobalNamespace))
    {
      foreach (INamedTypeSymbol type in ns.GetTypeMembers())
      {
        bool hasApiEndpoint = type.GetAttributes()
          .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, apiEndpointAttribute));
        if (!hasApiEndpoint)
        {
          continue;
        }

        INamedTypeSymbol? requestClass = type.GetTypeMembers()
          .FirstOrDefault(m => m.Name is "Query" or "Command");
        if (requestClass is null)
        {
          continue;
        }

        // [ClientOnlyContract] excludes a contract from server hosting, so it must never gain an
        // ingress carve-out. Check BOTH the outer type and the nested request: the real convention
        // places it on the nested Query/Command, but either placement means "not hosted".
        bool isClientOnly = HasClientOnlyContract(type) || HasClientOnlyContract(requestClass);
        if (isClientOnly)
        {
          continue;
        }

        AttributeData? apiRoute = requestClass.GetAttributes()
          .FirstOrDefault(attr => attr.AttributeClass?.Name == ApiRouteAttributeSimpleName);
        if (apiRoute is null || apiRoute.ConstructorArguments.Length == 0)
        {
          continue;
        }

        string? template = apiRoute.ConstructorArguments[0].Value?.ToString();
        if (!string.IsNullOrWhiteSpace(template))
        {
          yield return new HostedRoute(template!, assembly.Name);
        }
      }
    }
  }

  private static bool HasClientOnlyContract(INamedTypeSymbol symbol)
    => symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == ClientOnlyContractAttributeSimpleName);

  private static IEnumerable<INamespaceSymbol> GetAllNamespaces(INamespaceSymbol root)
  {
    yield return root;
    foreach (INamespaceSymbol child in root.GetNamespaceMembers())
    {
      foreach (INamespaceSymbol descendant in GetAllNamespaces(child))
      {
        yield return descendant;
      }
    }
  }

  private static string BuildSource(SortedSet<string> prefixes)
  {
    string body = prefixes.Count == 0
      ? "ImmutableArray<string>.Empty"
      : "ImmutableArray.Create(\n"
        + string.Join(",\n", prefixes.Select(p => $"    \"{p}\"")) + ")";

    return $$"""
      // <auto-generated/>
      #nullable enable
      using System.Collections.Immutable;

      /// <summary>
      /// Top-level '/api/&lt;segment&gt;' prefixes owned by Web.Server, derived from the hosted
      /// web-contracts [ApiRoute] templates (task 107). The YARP ingress loops over <see cref="All"/>
      /// to declare its Web.Server carve-outs, so the ingress cannot drift from the contracts.
      /// Emitted in the global namespace on purpose (see IngressRoutePrefixGenerator Design region).
      /// </summary>
      public static class WebServerApiRoutePrefixes
      {
        /// <summary>The distinct top-level api prefixes, sorted ordinally.</summary>
        public static readonly ImmutableArray<string> All = {{body}};
      }
      """;
  }

  private readonly struct HostedRoute
  {
    public HostedRoute(string template, string assemblyName)
    {
      Template = template;
      AssemblyName = assemblyName;
    }

    public string Template { get; }
    public string AssemblyName { get; }
  }

  private sealed class GenerationOptions
  {
    private GenerationOptions(bool enabled, ImmutableHashSet<string> sourceAssemblies, ImmutableArray<string> reservedPrefixes)
    {
      Enabled = enabled;
      SourceAssemblies = sourceAssemblies;
      ReservedPrefixes = reservedPrefixes;
    }

    public bool Enabled { get; }
    public ImmutableHashSet<string> SourceAssemblies { get; }
    public ImmutableArray<string> ReservedPrefixes { get; }

    public static GenerationOptions Read(AnalyzerConfigOptions globalOptions)
    {
      bool enabled = false;
      if (globalOptions.TryGetValue("build_property.EnableIngressRouteGeneration", out string? enabledValue)
        && bool.TryParse(enabledValue, out bool parsed))
      {
        enabled = parsed;
      }

      var sourceAssemblies = ReadList(globalOptions, "build_property.IngressWebContractAssemblies")
        .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

      var reservedPrefixes = ReadList(globalOptions, "build_property.IngressReservedPathPrefixes")
        .ToImmutableArray();

      return new GenerationOptions(enabled, sourceAssemblies, reservedPrefixes);
    }

    private static IEnumerable<string> ReadList(AnalyzerConfigOptions globalOptions, string key)
    {
      if (globalOptions.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
      {
        return value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries)
          .Select(part => part.Trim())
          .Where(part => part.Length > 0);
      }

      return Array.Empty<string>();
    }
  }
}
