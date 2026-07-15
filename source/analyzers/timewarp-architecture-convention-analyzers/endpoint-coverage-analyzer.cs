#region Purpose
// Enforces contract<->endpoint agreement in server projects: verbs match (TWA0005) and every
// routed contract is served or explicitly opted out (TWA0006).
#endregion

#region Design
// Targets the 405 bug class: a contract existed with no server endpoint, and its sibling —
// endpoint verb drifting from the contract's [ApiRoute] verb. Both diagnostics share one
// compilation walk (source endpoint types vs referenced contract metadata).
// Scope gates keep this quiet everywhere else: the analyzer only acts when the compilation
// declares at least one BaseEndpoint/BaseFastEndpoint subclass (i.e. is a server project), and
// contract discovery walks only referenced assemblies named *contracts* that share the server's
// first name segment (web-server <-> web-contracts) — a server referencing another server's
// contracts as a CLIENT (web-server uses api-contracts for the SPA) must not vouch for them.
// TWA0005 applies to MVC BaseEndpoint subclasses only: FastEndpoints are source-generated FROM
// the contract's verb, so drift is impossible there. Contracts using the manual IApiRequest form
// (no [ApiRoute]) are invisible to both diagnostics by design — the attribute is the contract of
// enforcement. [ClientOnlyContract(reason)] is the explicit TWA0006 opt-out.
// TWA0006 reports with no location (the defect is an absence); suppress per-contract via the
// attribute, not pragmas.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointCoverageAnalyzer : DiagnosticAnalyzer
{
  public const string VerbMismatchId = "TWA0005";
  public const string MissingEndpointId = "TWA0006";

  private const string Category = "Design";

  private static readonly DiagnosticDescriptor VerbMismatch =
    new
    (
      VerbMismatchId,
      title: "Endpoint HTTP verb does not match the contract's [ApiRoute] verb",
      messageFormat: "Endpoint '{0}' uses [{1}] but contract '{2}' declares HttpVerb.{3}",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The contract's [ApiRoute] verb is the single authority; an endpoint attribute that disagrees produces 405s the client cannot anticipate.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

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
    ImmutableArray.Create(VerbMismatch, MissingEndpoint);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationAction(Analyze);
  }

  private static void Analyze(CompilationAnalysisContext context)
  {
    INamedTypeSymbol? baseEndpoint = context.Compilation.GetTypeByMetadataName("TimeWarp.Foundation.Features.BaseEndpoint`2");
    INamedTypeSymbol? baseFastEndpoint = context.Compilation.GetTypeByMetadataName("TimeWarp.Foundation.Features.BaseFastEndpoint`2");
    if (baseEndpoint is null && baseFastEndpoint is null) return;

    // Collect endpoint subclasses declared (or source-generated) in THIS compilation.
    var covered = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
    var mvcEndpoints = new List<(INamedTypeSymbol Endpoint, INamedTypeSymbol Request)>();

    foreach (INamedTypeSymbol type in GetAllTypes(context.Compilation.Assembly.GlobalNamespace))
    {
      for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
      {
        INamedTypeSymbol original = baseType.OriginalDefinition;
        bool isMvc = SymbolEqualityComparer.Default.Equals(original, baseEndpoint);
        bool isFast = SymbolEqualityComparer.Default.Equals(original, baseFastEndpoint);
        if (!isMvc && !isFast) continue;

        if (baseType.TypeArguments[0] is INamedTypeSymbol request)
        {
          covered.Add(request);
          if (isMvc) mvcEndpoints.Add((type, request));
        }

        break;
      }
    }

    if (covered.Count == 0) return;   // not a server project

    // TWA0005 — MVC endpoint [HttpX] must match the contract's [ApiRoute] verb.
    foreach ((INamedTypeSymbol endpoint, INamedTypeSymbol request) in mvcEndpoints)
    {
      string? contractVerb = GetApiRouteVerb(request, out _);
      if (contractVerb is null) continue;   // manual-route contract: no [ApiRoute], nothing to compare

      foreach (IMethodSymbol method in endpoint.GetMembers().OfType<IMethodSymbol>())
      {
        foreach (AttributeData attribute in method.GetAttributes())
        {
          string? endpointVerb = HttpAttributeVerb(attribute);
          if (endpointVerb is null) continue;

          if (!string.Equals(endpointVerb, contractVerb, System.StringComparison.OrdinalIgnoreCase))
          {
            Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
              ?? endpoint.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(
              VerbMismatch, location, endpoint.Name, $"Http{endpointVerb}", request.ToDisplayString(), contractVerb));
          }
        }
      }
    }

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
      foreach (INamedTypeSymbol type in GetAllTypes(assembly.GlobalNamespace))
      {
        string? verb = GetApiRouteVerb(type, out string? route);
        if (verb is null) continue;

        if (type.GetAttributes().Any(static a => a.AttributeClass?.Name == "ClientOnlyContractAttribute")) continue;

        if (!covered.Contains(type))
        {
          context.ReportDiagnostic(Diagnostic.Create(
            MissingEndpoint, Location.None, type.ToDisplayString(), route ?? "?", verb));
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

  private static string? GetApiRouteVerb(INamedTypeSymbol type, out string? route)
  {
    route = null;
    AttributeData? apiRoute = type.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "ApiRouteAttribute");
    if (apiRoute is null || apiRoute.ConstructorArguments.Length < 2) return null;

    route = apiRoute.ConstructorArguments[0].Value?.ToString();
    TypedConstant verbArgument = apiRoute.ConstructorArguments[1];

    // Resolve the enum member name from its constant value (metadata stores the underlying int).
    if (verbArgument.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
    {
      IFieldSymbol? member = enumType.GetMembers().OfType<IFieldSymbol>()
        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, verbArgument.Value));
      if (member is not null) return member.Name;
    }

    return verbArgument.Value?.ToString();
  }

  // HttpGetAttribute -> "Get"; anything not shaped Http*Attribute -> null.
  private static string? HttpAttributeVerb(AttributeData attribute)
  {
    string? name = attribute.AttributeClass?.Name;
    if (name is null || !name.StartsWith("Http", System.StringComparison.Ordinal)
      || !name.EndsWith("Attribute", System.StringComparison.Ordinal))
    {
      return null;
    }

    string verb = name.Substring("Http".Length, name.Length - "Http".Length - "Attribute".Length);
    return verb is "Get" or "Post" or "Put" or "Delete" or "Patch" or "Head" or "Options" ? verb : null;
  }

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
  {
    foreach (INamespaceOrTypeSymbol member in root.GetMembers())
    {
      switch (member)
      {
        case INamespaceSymbol ns:
          foreach (INamedTypeSymbol nested in GetAllTypes(ns)) yield return nested;
          break;
        case INamedTypeSymbol type:
          yield return type;
          foreach (INamedTypeSymbol nested in AllNested(type)) yield return nested;
          break;
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol> AllNested(INamedTypeSymbol type)
  {
    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
    {
      yield return nested;
      foreach (INamedTypeSymbol deeper in AllNested(nested)) yield return deeper;
    }
  }
}
