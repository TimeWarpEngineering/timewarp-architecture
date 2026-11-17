#region Purpose
// Constants, platform package list and the variant matrix for dev template-smoke.
#endregion
#region Design
// Regression gate for task 115: template sourceName is TimeWarp.Architecture, so package-id literals
// TimeWarp.Architecture.* used to rewrite to SmokeDefault.* and break restore. Smoke always generates
// with names different from sourceName (SmokeDefault, SmokeNoPostgres, ...) so rewrite bugs surface.
// Matrix: defaults plus each flag off on its own (task 373 added web, grpc and yarp - a flag nobody
// generates off is a flag whose off output nobody builds).
// Packs the template AND monorepo platform packages (foundation + analyzers/generators/attributes +
// modules + identity) into a local feed. TimeWarp.Contracts restores from nuget.org at the
// Directory.Packages.props pin. Published nuget.org pins lag monorepo API surface; smoke validates
// "this branch's template + this branch's packages".
// Task 375: the loop runs SmokeDefault alone unless --matrix selects every entry.
#endregion

namespace DevCli.Services;

/// <summary>
/// Static configuration for template-smoke: pack list, smoke version, variants, removed symbols.
/// </summary>
internal static class TemplateSmokeMatrix
{
  public const string TemplateProject =
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  public const string TemplatePackageId = "TimeWarp.Architecture";
  public const string TemplateShortName = "timewarp-architecture";

  /// <summary>
  /// Unique version for smoke-local packs so the global NuGet cache cannot shadow them with
  /// same-version nuget.org bits that lag monorepo API surface (Entity, EndpointAllowAnonymous, …).
  /// Generated Directory.Packages.props pins for these PackageIds are rewritten to this version.
  /// </summary>
  public const string SmokePackageVersion = "2.0.0-smoke";

  public static readonly string[] PlatformPackageProjects =
  [
    "source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj",
    "source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj",
    "source/analyzers/timewarp-architecture-analyzers/timewarp-architecture-analyzers.csproj",
    "source/foundation/foundation-domain/foundation-domain.csproj",
    "source/foundation/foundation-contracts/foundation-contracts.csproj",
    "source/foundation/foundation-application/foundation-application.csproj",
    "source/foundation/foundation-infrastructure/foundation-infrastructure.csproj",
    "source/foundation/foundation-server/foundation-server.csproj",
    "source/libraries/timewarp-modules/timewarp-modules.csproj",
    // Identity is always package-mode for generated apps (task 126-004). Without a smoke-local
    // pack, generated apps would pull nuget.org's TimeWarp.Identity whose Foundation dependency
    // floor (>= the published version) collides with the 2.0.0-smoke pins → NU1603 under
    // -warnaserror.
    "source/libraries/timewarp-identity/timewarp-identity.csproj",
    // TimeWarp.402 (task 104-007): same dual-mode exclude as identity; pack when apps reference it.
    "source/libraries/timewarp-402/timewarp-402.csproj",
  ];

  // ExcludedFamilies drives flag-off assertions in the co-located-test tiers: for a family
  // listed here, tiers 1/3 assert its test artifacts are ABSENT from the generated app
  // (task 136 review R2-1 — an orphaned aggregator under a flag-off generation must fail smoke).
  public static readonly (string Name, string[] ExtraArgs, string[] ExcludedFamilies)[] SmokeMatrix =
  [
    ("SmokeDefault", [], []),
    ("SmokeNoPostgres", ["--postgres", "false"], []),
    ("SmokeNoApi", ["--api", "false"], ["api"]),
    ("SmokeNoWeb", ["--web", "false"], ["web"]),
    ("SmokeNoGrpc", ["--grpc", "false"], []),
    ("SmokeNoYarp", ["--yarp", "false"], []),
  ];

  // Task 288: generate-only (no build) — a dotted name must reach the AppHost deploy parameters as a
  // DNS-1123 kebab name through the appNameKebab derived symbol.
  public const string DottedSmokeName = "Contoso.Shop";
  public const string DottedSmokeKebabName = "contoso-shop";

  public static readonly string[] RemovedTemplateSymbols =
  [
    "foundationPackages",
    "analyzerPackages",
    "identityPackages",
  ];
}
