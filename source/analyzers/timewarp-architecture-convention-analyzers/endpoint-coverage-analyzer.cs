#region Purpose
// Enforces TWA0006: every routed contract in a server project is served or explicitly opted out.
#endregion

#region Design
// Targets the 405 bug class: a contract with [ApiRoute] but no server endpoint.
// Scope gates keep this quiet outside servers: the analyzer only acts when the compilation
// declares at least one BaseFastEndpoint subclass, and contract discovery walks only
// referenced assemblies named *contracts* that share the server's first name segment
// (web-server <-> web-contracts) — a server referencing another server's contracts as a
// CLIENT (web-server uses api-contracts for the SPA) must not vouch for them.
// TWA0005 (MVC verb mismatch) was retired with BaseEndpoint (task 131 F-002): FastEndpoints
// are source-generated from the contract's [ApiRoute] verb, so hand-written verb drift cannot
// occur. ID TWA0005 is reserved and must not be reused. Contracts using the manual IApiRequest
// form (no [ApiRoute]) are invisible by design — the attribute is the contract of enforcement.
// [ClientOnlyContract(reason)] is the explicit TWA0006 opt-out — on the routed type OR its
// containing outer operation (F-004). Reports with no location (the defect is an absence);
// suppress per-contract via the attribute, not pragmas.
// Discovery via HostedRouteDiscovery (linked shared source shared with generators).
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointCoverageAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Retired with MVC BaseEndpoint (task 131 F-002). Do not reuse this ID.</summary>
  public const string VerbMismatchId = "TWA0005";

  public const string MissingEndpointId = "TWA0006";

  private const string Category = "Design";

  private static readonly DiagnosticDescriptor MissingEndpoint =
    new
    (
      MissingEndpointId,
      title: "Routed contract has no server endpoint",
      messageFormat: "Contract '{0}' declares route '{1}' ({2}) but no endpoint in this project serves it; add an endpoint or mark the contract [ClientOnlyContract(reason)]",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A contract with [ApiRoute] promises a server endpoint. Absence is the 405 bug class; deliberate absence must be declared with [ClientOnlyContract].",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(MissingEndpoint);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationAction(Analyze);
  }

  private static void Analyze(CompilationAnalysisContext context)
  {
    INamedTypeSymbol? baseFastEndpoint = context.Compilation.GetTypeByMetadataName("TimeWarp.Foundation.Features.BaseFastEndpoint`2");
    if (baseFastEndpoint is null) return;

    // Collect endpoint subclasses declared (or source-generated) in THIS compilation.
    var covered = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

    foreach (INamedTypeSymbol type in HostedRouteDiscovery.GetAllTypes(context.Compilation.Assembly.GlobalNamespace))
    {
      for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
      {
        INamedTypeSymbol original = baseType.OriginalDefinition;
        if (!SymbolEqualityComparer.Default.Equals(original, baseFastEndpoint)) continue;

        if (baseType.TypeArguments[0] is INamedTypeSymbol request)
        {
          covered.Add(request);
        }

        break;
      }
    }

    if (covered.Count == 0) return;   // not a server project

    // TWA0006 — every routed contract in the PAIRED contracts assemblies must be covered.
    // Pairing = shared first name segment (web-server <-> web-contracts): a server may reference
    // another server's contracts as a client (web-server references api-contracts for the SPA)
    // and must not vouch for those.
    string sourcePrefix = FirstSegment(context.Compilation.Assembly.Name);
    IEnumerable<IAssemblySymbol> contractAssemblies = context.Compilation.SourceModule.ReferencedAssemblySymbols
      .Where(a => a.Name.Contains("contracts", System.StringComparison.OrdinalIgnoreCase)
        && string.Equals(FirstSegment(a.Name), sourcePrefix, System.StringComparison.OrdinalIgnoreCase))
      .Concat(new[] { (IAssemblySymbol)context.Compilation.Assembly });

    foreach (IAssemblySymbol assembly in contractAssemblies)
    {
      foreach (INamedTypeSymbol type in HostedRouteDiscovery.GetAllTypes(assembly.GlobalNamespace))
      {
        if (!HostedRouteDiscovery.TryGetRoutedRequest(type, out RoutedRequestInfo routed))
        {
          continue;
        }

        if (!covered.Contains(type))
        {
          context.ReportDiagnostic(Diagnostic.Create(
            MissingEndpoint, Location.None, type.ToDisplayString(), routed.RouteTemplate, routed.HttpVerbName));
        }
      }
    }
  }

  // "web-server" -> "web"; "Web.Contracts" -> "Web".
  private static string FirstSegment(string assemblyName)
  {
    int cut = assemblyName.IndexOfAny(['-', '.']);
    return cut < 0 ? assemblyName : assemblyName.Substring(0, cut);
  }
}
