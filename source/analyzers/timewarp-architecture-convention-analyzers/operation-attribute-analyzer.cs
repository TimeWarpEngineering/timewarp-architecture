#region Purpose
// Requires [Operation] with a Description (TWA0035, warning) on a hosted contract's request, so
// every hosted operation has a transport-neutral name and summary.
#endregion

#region Design
// Task 386: TimeWarp.Contracts gives a contract one name and description that HTTP, CLI, and MCP
// hosts share. Task 458-005 raised the rule from info to warning (warnings are errors) once every
// hosted contract carried one: the OpenAPI operationId is the [Operation] name and the OpenAPI
// summary is its Description, so a contract without either publishes an unnamed or
// unsummarized operation. A missing, empty, or whitespace Description is reported like a missing
// attribute.
// Hosted = HostedRouteDiscovery.TryGetHostedOperation, the same gate the FastEndpoint and
// ingress generators use: [ApiEndpoint] outer type, nested Query|Command with [ApiRoute], not
// [ClientOnlyContract]. Unrouted and client-only contracts are never reported.
// [Operation] is matched by FQN (TimeWarp.Contracts.OperationAttribute), the name the contracts
// generator and EndpointEmitModel read, so a foreign OperationAttribute does not count. A
// whitespace name counts as missing: the generators treat it as no operation, and the attribute
// constructor throws on it. One descriptor carries both cases; the second message argument says
// which part is missing.
// Location = the request class identifier on the declaration that carries [ApiRoute], which is
// where the attribute goes. Generated partial declarations are never the location.
// SymbolAction on NamedType, like EndpointAuthPostureAnalyzer: one contract's own attributes.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Roslyn analyzer for TWA0035: a hosted contract's request has no [Operation] or no Description.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OperationAttributeAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0035 (hosted contract without [Operation] or its Description).</summary>
  public const string MissingOperationId = "TWA0035";

  private const string Category = "Design";
  private const string OperationAttributeFullName = "TimeWarp.Contracts.OperationAttribute";

  private static readonly DiagnosticDescriptor MissingOperation =
    new
    (
      MissingOperationId,
      title: "Hosted contract has no [Operation] with a Description",
      messageFormat: "'{0}' is hosted but {1} — add [Operation(\"<resource>.<action>\", Description = \"...\")] so HTTP, CLI, and MCP share one operation name and summary",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "TimeWarp.Contracts' [Operation] gives a request its transport-neutral name and description. The OpenAPI operationId is the name and the OpenAPI summary is the Description, and CLI and MCP hosts read both. A whitespace name or Description counts as missing."
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

    string? missing = MissingPart(info.RequestType);
    if (missing is null)
      return;

    Location location = info.ApiRouteAttribute.ApplicationSyntaxReference?
        .GetSyntax(context.CancellationToken)
        .FirstAncestorOrSelf<ClassDeclarationSyntax>()?
        .Identifier.GetLocation()
      ?? info.RequestType.Locations.FirstOrDefault()
      ?? Location.None;

    context.ReportDiagnostic(Diagnostic.Create(MissingOperation, location, info.RequestType.ToDisplayString(), missing));
  }

  /// <summary>Null when the request has a named [Operation] with a Description; else what is missing.</summary>
  private static string? MissingPart(INamedTypeSymbol requestType)
  {
    AttributeData? operation = requestType.GetAttributes().FirstOrDefault(static attr =>
      HostedRouteDiscovery.HasMetadataName(attr.AttributeClass, OperationAttributeFullName)
      && attr.ConstructorArguments.Length > 0
      && attr.ConstructorArguments[0].Value is string name
      && !string.IsNullOrWhiteSpace(name));
    if (operation is null)
      return "has no [Operation]";

    bool hasDescription = operation.NamedArguments.Any(static argument =>
      argument.Key == "Description"
      && argument.Value.Value is string description
      && !string.IsNullOrWhiteSpace(description));
    return hasDescription ? null : "its [Operation] has no Description";
  }
}
