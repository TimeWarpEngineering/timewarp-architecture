#region Purpose
// Shared template smoke/publish-smoke harness: composes the smoke helpers over one repo root.
#endregion
#region Design
// Task 131-003 / F-007: collapse duplication between template-smoke and template-publish-smoke.
// Task 469-002: each responsibility is its own type, built once here so they share the suffix cache:
// TemplateSmokeArchitectureSuffixes (props SSOT derivation), TemplateSmokePlatformPins (rewrite and
// namespace-literal asserts), TemplateSmokeGeneratedContent (skills, kanban, deploy parameters,
// ingress port), TemplateSmokeTestRuns (co-located tests, Jaribu aggregators, generated suites),
// TemplateSmokeGeneration (generate + SmokeOneAsync + environment), and
// TemplateSmokeInitializerAssets (host-free import-graph check).
#endregion

namespace DevCli.Services;

/// <summary>
/// Composition root for the template smoke gates' shared helpers.
/// </summary>
internal sealed class TemplateSmokeHarness
{
  public TemplateSmokeHarness(ITerminal terminal, string repoRoot)
  {
    Suffixes = new TemplateSmokeArchitectureSuffixes(terminal, repoRoot);
    Pins = new TemplateSmokePlatformPins(terminal, repoRoot, Suffixes);
    Content = new TemplateSmokeGeneratedContent(terminal, repoRoot);
    TestRuns = new TemplateSmokeTestRuns(terminal, repoRoot);
    Generation = new TemplateSmokeGeneration(terminal, repoRoot, Pins);
    InitializerAssets = new TemplateSmokeInitializerAssets(terminal);
  }

  public TemplateSmokeArchitectureSuffixes Suffixes { get; }

  public TemplateSmokePlatformPins Pins { get; }

  public TemplateSmokeGeneratedContent Content { get; }

  public TemplateSmokeTestRuns TestRuns { get; }

  public TemplateSmokeGeneration Generation { get; }

  public TemplateSmokeInitializerAssets InitializerAssets { get; }
}
