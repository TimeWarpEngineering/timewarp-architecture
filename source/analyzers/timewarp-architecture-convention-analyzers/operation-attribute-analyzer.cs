#region Purpose
// Suggests [Operation] (TWA0035, info) on a hosted contract's request that has no
// transport-neutral operation name.
#endregion

#region Design
// Task 386: TimeWarp.Contracts gives a contract one name and description that HTTP, CLI, and MCP
// hosts share. Hosted contracts without one still generate an HTTP endpoint, so this is a
// suggestion, not a warning: the build stays clean while contracts adopt [Operation], and a
// later task can raise the severity.
// Hosted = HostedRouteDiscovery.TryGetHostedOperation, the same gate the FastEndpoint and
// ingress generators use: [ApiEndpoint] outer type, nested Query|Command with [ApiRoute], not
// [ClientOnlyContract]. Unrouted and client-only contracts are never reported.
// [Operation] is matched by FQN (TimeWarp.Contracts.OperationAttribute), the name the contracts
// generator and EndpointEmitModel read, so a foreign OperationAttribute does not count. A
// whitespace name counts as missing: the generators treat it as no operation, and the attribute
// constructor throws on it.
// Location = the request class identifier on the declaration that carries [ApiRoute], which is
// where the attribute goes. Generated partial declarations are never the location.
// SymbolAction on NamedType, like EndpointAuthPostureAnalyzer: one contract's own attributes.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Roslyn analyzer for TWA0035: a hosted contract's request has no [Operation].</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OperationAttributeAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0035 (hosted contract without [Operation]).</summary>
  public const string MissingOperationId = "TWA0035";

  private const string Category = "Design";
  private const string OperationAttributeFullName = "TimeWarp.Contracts.OperationAttribute";

  private static readonly DiagnosticDescriptor MissingOperation =
    new
    (
      MissingOperationId,
      title: "Hosted contract has no [Operation]",
      messageFormat: "'{0}' is hosted but has no [Operation] — add [Operation(\"name\", Description = ...)] so HTTP, CLI, and MCP share one operation name",
      Category,
      DiagnosticSeverity.Info,
      isEnabledByDefault: true,
      description: "TimeWarp.Contracts' [Operation] gives a request its transport-neutral name and description. The HTTP endpoint is generated without it, but CLI and MCP hosts and the operation catalog need it. A whitespace name counts as missing."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(MissingOperation);

  /// <summary>Registers the symbol action that reports TWA0035.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
  }

  private static void Analyze(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;
    if (!HostedRouteDiscovery.TryGetHostedOperation(type, out HostedOperationInfo info))
      return;

    if (HasOperationName(info.RequestType))
      return;

    Location location = info.ApiRouteAttribute.ApplicationSyntaxReference?
        .GetSyntax(context.CancellationToken)
        .FirstAncestorOrSelf<ClassDeclarationSyntax>()?
        .Identifier.GetLocation()
      ?? info.RequestType.Locations.FirstOrDefault()
      ?? Location.None;

    context.ReportDiagnostic(Diagnostic.Create(MissingOperation, location, info.RequestType.ToDisplayString()));
  }

  private static bool HasOperationName(INamedTypeSymbol requestType) =>
    requestType.GetAttributes().Any(static attr =>
      HostedRouteDiscovery.HasMetadataName(attr.AttributeClass, OperationAttributeFullName)
      && attr.ConstructorArguments.Length > 0
      && attr.ConstructorArguments[0].Value is string name
      && !string.IsNullOrWhiteSpace(name));
}
