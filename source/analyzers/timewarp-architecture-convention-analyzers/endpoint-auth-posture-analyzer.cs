#region Purpose
// Enforces that every generated FastEndpoint contract states its auth posture explicitly: neither
// marker present (TWA0013), a contradictory pairing of markers (TWA0014), or [ApiEndpoint] combined
// with [ClientOnlyContract] (TWA0020).
#endregion

#region Design
// Task 110: the FastEndpoint generator's default flipped fail-closed (no marker -> emit nothing ->
// FastEndpoints requires authentication by default). This analyzer is what makes that default
// unreachable in a clean build — every [ApiEndpoint] contract must carry exactly one of
// [EndpointAuthorize] / [EndpointAllowAnonymous], so "what does this endpoint require" is always a
// stated fact on the contract, never an emergent property of what the generator happened to do with
// silence.
// TWA0013 — missing posture: [ApiEndpoint] present, neither marker present.
// TWA0014 — conflicting posture, two distinct shapes:
//   (a) BOTH [EndpointAuthorize] and [EndpointAllowAnonymous] present — contradictory on their
//       face; the generator picks a deterministic winner ([EndpointAuthorize]), but the contract
//       author must resolve the contradiction, not rely on that tiebreak.
//   (b) [EndpointAllowAnonymous] present while the nested Query/Command declares
//       IAuthApiRequest — either the manual interface form (: IAuthApiRequest, detected via
//       AllInterfaces by SIMPLE NAME) or the [AuthApiRequest] mixin attribute form. A contract
//       whose Query/Command says "I carry an authenticated user's identity" but whose endpoint
//       says "anyone may call this anonymously" is the contradiction task 110 surfaces —
//       IAuthApiRequest is a CLIENT/mock-mode identity signal only and does not secure the server.
// TWA0020 — [ApiEndpoint] + [ClientOnlyContract] (outer or nested Query/Command): generators skip
// ClientOnly contracts (not hosted), so the generation opt-in contradicts the client-only opt-out.
// Location = the [ApiEndpoint] attribute application — the contract's own declaration is where an
// author reads the diagnostic and adds the missing/corrected marker.
// Registered as a SymbolAction on NamedType (not a CompilationAction like EndpointCoverageAnalyzer):
// this analyzer's scope is a single contract type's own attributes, not a whole-compilation
// cross-reference walk, so the finer-grained per-symbol registration is both simpler and (per
// Roslyn's incremental analyzer model) cheaper to re-run on unrelated edits.
// ClientOnly checks use HostedRouteDiscovery (linked shared source).
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointAuthPostureAnalyzer : DiagnosticAnalyzer
{
  public const string MissingPostureId = "TWA0013";
  public const string ConflictingPostureId = "TWA0014";
  public const string ClientOnlyContradictionId = "TWA0020";

  private const string Category = "Design";

  private static readonly DiagnosticDescriptor MissingPosture =
    new
    (
      MissingPostureId,
      title: "Generated endpoint has no stated auth posture",
      messageFormat: "Contract '{0}' carries [ApiEndpoint] but neither [EndpointAuthorize] nor [EndpointAllowAnonymous] — add one so the generated endpoint's auth posture is a stated fact, not the fail-closed default",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The FastEndpoint generator fails closed (task 110): an [ApiEndpoint] contract with neither marker generates an endpoint FastEndpoints will require authentication for, but the contract's own intent is silent. Add [EndpointAuthorize(...)] for a protected route or [EndpointAllowAnonymous(reason)] for a deliberately public one."
    );

  private static readonly DiagnosticDescriptor ConflictingPosture =
    new
    (
      ConflictingPostureId,
      title: "Generated endpoint's auth posture is contradictory",
      messageFormat: "Contract '{0}' has a contradictory auth posture — {1}; resolve on the contract, do not rely on the generator's [EndpointAuthorize]-wins tiebreak",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Either both [EndpointAuthorize] and [EndpointAllowAnonymous] are present on the same contract, or [EndpointAllowAnonymous] is paired with a nested Query/Command that declares IAuthApiRequest (interface or [AuthApiRequest] mixin) — an auth-intent request marked anonymous at the endpoint. Resolve the contradiction; do not rely on the generator's [EndpointAuthorize]-wins tiebreak."
    );

  private static readonly DiagnosticDescriptor ClientOnlyContradiction =
    new
    (
      ClientOnlyContradictionId,
      title: "[ApiEndpoint] combined with [ClientOnlyContract]",
      messageFormat: "Contract '{0}' carries both [ApiEndpoint] and [ClientOnlyContract] ({1}) — generators skip ClientOnly contracts, so remove [ApiEndpoint] or remove [ClientOnlyContract]",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Task 131-001 F-004: [ApiEndpoint] opts a contract into FastEndpoint generation; [ClientOnlyContract] opts it out of server hosting (TWA0006). Both on the same operation (outer or nested Query/Command) is a contradiction — generators skip ClientOnly and TWA0006 treats it as coverage opt-out."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(MissingPosture, ConflictingPosture, ClientOnlyContradiction);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
  }

  private static void Analyze(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;

    AttributeData? apiEndpoint = type.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "ApiEndpointAttribute");
    if (apiEndpoint is null) return;

    Location location = apiEndpoint.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
      ?? type.Locations.FirstOrDefault() ?? Location.None;

    INamedTypeSymbol? requestType = type.GetTypeMembers()
      .FirstOrDefault(static m => m.Name is "Query" or "Command");

    // TWA0020 — [ApiEndpoint] + [ClientOnlyContract] on outer or nested.
    bool clientOnlyOuter = HostedRouteDiscovery.HasClientOnly(type);
    bool clientOnlyNested = requestType is not null && HostedRouteDiscovery.HasClientOnly(requestType);
    if (clientOnlyOuter || clientOnlyNested)
    {
      string where = clientOnlyOuter
        ? "on the outer type"
        : $"on nested {requestType!.Name}";
      context.ReportDiagnostic(Diagnostic.Create(
        ClientOnlyContradiction,
        location,
        type.ToDisplayString(),
        where));
      // Still useful to surface auth-posture issues after ClientOnly is resolved, but stop here so
      // authors fix the hosting contradiction first without diagnostic noise.
      return;
    }

    AttributeData? endpointAuthorize = type.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "EndpointAuthorizeAttribute");
    AttributeData? endpointAllowAnonymous = type.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "EndpointAllowAnonymousAttribute");

    // TWA0014(a) — both markers present.
    if (endpointAuthorize is not null && endpointAllowAnonymous is not null)
    {
      context.ReportDiagnostic(Diagnostic.Create(
        ConflictingPosture,
        location,
        type.ToDisplayString(),
        "it carries both [EndpointAuthorize] and [EndpointAllowAnonymous]; remove one"));
      return;
    }

    // TWA0013 — neither marker present.
    if (endpointAuthorize is null && endpointAllowAnonymous is null)
    {
      context.ReportDiagnostic(Diagnostic.Create(MissingPosture, location, type.ToDisplayString()));
      return;
    }

    // TWA0014(b) — [EndpointAllowAnonymous] while the nested Query/Command declares IAuthApiRequest.
    if (endpointAllowAnonymous is not null && requestType is not null && DeclaresAuthApiRequest(requestType))
    {
      context.ReportDiagnostic(Diagnostic.Create(
        ConflictingPosture,
        location,
        type.ToDisplayString(),
        $"it carries [EndpointAllowAnonymous] but its {requestType.Name} declares IAuthApiRequest; add [EndpointAuthorize] or remove the auth-intent marker from {requestType.Name}"));
    }
  }

  private static bool DeclaresAuthApiRequest(INamedTypeSymbol requestType)
  {
    bool implementsInterface = requestType.AllInterfaces.Any(static i => i.Name == "IAuthApiRequest");
    bool hasMixinAttribute = requestType.GetAttributes()
      .Any(static a => a.AttributeClass?.Name == "AuthApiRequestAttribute");

    return implementsInterface || hasMixinAttribute;
  }
}
