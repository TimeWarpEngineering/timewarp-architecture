#region Purpose
// Enforces that every generated FastEndpoint contract states its auth posture explicitly: neither
// marker present (TWA0013) or a contradictory pairing of markers (TWA0014).
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
//       AllInterfaces by SIMPLE NAME, matching the repo's established cross-assembly-symbol
//       convention — see EndpointCoverageAnalyzer/EndpointMetadata.FromSymbol for the same pattern)
//       or the [AuthApiRequest] mixin attribute form (foundation-contracts-generators'
//       ContractsMixinGenerator expands this into BOTH an IAuthApiRequest interface
//       implementation AND leaves the attribute application on the class — this analyzer checks
//       for EITHER independently, by simple name, so it does not depend on generator execution
//       order relative to this analyzer within the same compilation pass). A contract whose
//       Query/Command says "I carry an authenticated user's identity" but whose endpoint says
//       "anyone may call this anonymously" is exactly the contradiction task 110 exists to surface
//       — IAuthApiRequest is a CLIENT/mock-mode identity signal only (see the web-api-contracts
//       skill's three-state truth table) and does not, by itself, secure the server; pairing it
//       with an anonymous endpoint marker either means the marker is wrong or the interface should
//       not be there.
// Location = the [ApiEndpoint] attribute application (mirrors EndpointCoverageAnalyzer's
// verb-mismatch location choice) — the contract's own declaration is where an author reads the
// diagnostic and adds the missing/corrected marker.
// Registered as a SymbolAction on NamedType (not a CompilationAction like EndpointCoverageAnalyzer):
// this analyzer's scope is a single contract type's own attributes, not a whole-compilation
// cross-reference walk, so the finer-grained per-symbol registration is both simpler and (per
// Roslyn's incremental analyzer model) cheaper to re-run on unrelated edits.
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointAuthPostureAnalyzer : DiagnosticAnalyzer
{
  public const string MissingPostureId = "TWA0013";
  public const string ConflictingPostureId = "TWA0014";

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
      messageFormat: "{0}",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Either both [EndpointAuthorize] and [EndpointAllowAnonymous] are present on the same contract, or [EndpointAllowAnonymous] is paired with a nested Query/Command that declares IAuthApiRequest (interface or [AuthApiRequest] mixin) — an auth-intent request marked anonymous at the endpoint. Resolve the contradiction; do not rely on the generator's [EndpointAuthorize]-wins tiebreak."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(MissingPosture, ConflictingPosture);

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

    AttributeData? endpointAuthorize = type.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "EndpointAuthorizeAttribute");
    AttributeData? endpointAllowAnonymous = type.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "EndpointAllowAnonymousAttribute");

    Location location = apiEndpoint.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
      ?? type.Locations.FirstOrDefault() ?? Location.None;

    // TWA0014(a) — both markers present.
    if (endpointAuthorize is not null && endpointAllowAnonymous is not null)
    {
      context.ReportDiagnostic(Diagnostic.Create(
        ConflictingPosture,
        location,
        $"Contract '{type.ToDisplayString()}' carries BOTH [EndpointAuthorize] and [EndpointAllowAnonymous] — remove one; [EndpointAuthorize] wins at generation but the contradiction must be resolved on the contract."));
      return;
    }

    // TWA0013 — neither marker present.
    if (endpointAuthorize is null && endpointAllowAnonymous is null)
    {
      context.ReportDiagnostic(Diagnostic.Create(MissingPosture, location, type.ToDisplayString()));
      return;
    }

    // TWA0014(b) — [EndpointAllowAnonymous] while the nested Query/Command declares IAuthApiRequest.
    if (endpointAllowAnonymous is not null)
    {
      INamedTypeSymbol? requestType = type.GetTypeMembers()
        .FirstOrDefault(static m => m.Name is "Query" or "Command");

      if (requestType is not null && DeclaresAuthApiRequest(requestType))
      {
        context.ReportDiagnostic(Diagnostic.Create(
          ConflictingPosture,
          location,
          $"Contract '{type.ToDisplayString()}' carries [EndpointAllowAnonymous] but its {requestType.Name} declares IAuthApiRequest (interface or [AuthApiRequest]) — an auth-intent request marked anonymous at the endpoint is contradictory; add [EndpointAuthorize] or remove the auth-intent marker from {requestType.Name}."));
      }
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
