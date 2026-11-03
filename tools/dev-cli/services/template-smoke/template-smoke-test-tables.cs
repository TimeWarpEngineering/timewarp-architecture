#region Purpose
// Co-located Jaribu runfile and family-aggregator tables the generated-app test tiers iterate.
#endregion
#region Design
// Task 135: co-located runfiles compile into no layer project (registered-unrouted "tests" suffix,
// feature-filename-grammar.json), so the ordinary solution build is blind to them; these tables name
// the files tiers 1-2 check. Task 136: aggregators are not in .slnx, so tier 3 runs them by directory.
// RequiredFamilies lets a flag-off entry assert the artifacts are ABSENT. A new family aggregator needs
// a JaribuFamilyAggregators entry.
#endregion

namespace DevCli.Services;

/// <summary>
/// Tables describing the generated-app test artifacts template-smoke asserts on.
/// </summary>
internal static class TemplateSmokeTestTables
{
  /// <summary>
  /// Co-located Jaribu runfiles (task 135) whose JARIBU_MULTI template-safety (the cnd:noEmit
  /// escape) and end-to-end runnability must survive <c>dotnet new</c> generation. These compile
  /// into NO layer project (registered-unrouted "tests" suffix, feature-filename-grammar.json) so
  /// the generated app's ordinary solution build is structurally blind to a regression here.
  /// Tiers 1–2 exercise standalone <c>dotnet run</c>; tier 3 (task 136) exercises the family
  /// <c>JARIBU_MULTI</c> aggregators via bare <c>dotnet test</c> from each project directory.
  /// </summary>
  public static readonly (string Family, string RelativePath)[] CoLocatedTestFiles =
  [
    ("web", "source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs"),
    ("api", "source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs"),
    // task 145-004 R2-3: hello is Web-only (#if(api) CreateWebWithApiAsync #else CreateWebAsync
    // #endif — R2-1) so tier 2 exercising it under SmokeNoApi is the acceptance test for that fix.
    ("web", "source/container-apps/web/features/hello/hello/hello-tests.cs"),
  ];

  /// <summary>
  /// Per-family JARIBU_MULTI aggregator projects (task 136), plus (task 145-002 R2-3) the
  /// timewarp-testing-tests suite project, which needs BOTH web and api. Relative to the
  /// generated app root. RequiredFamilies lets a flag-off smoke entry assert the artifacts are
  /// ABSENT (task 136 review R2-1) whenever ANY required family is excluded: an aggregator
  /// orphaned by a family flag would break the generated app, so absence is the pass condition.
  /// Also (task 145-002 R2-1) the acid test that the ContentRootPath fix holds for a
  /// MULTI-hosted-server consumer in a GENERATED app, not just this monorepo.
  /// </summary>
  public static readonly (string[] RequiredFamilies, string RelativeProjectDir)[] JaribuFamilyAggregators =
  [
    (["web"], "tests/container-apps/web/web-jaribu-tests"),
    (["api"], "tests/container-apps/api/api-jaribu-tests"),
    (["web", "api"], "tests/common/timewarp-testing-tests"),
  ];

  // The exact guarded lines a template-safe co-located runfile preamble must contain, verbatim,
  // in the GENERATED output. dotnet-new's conditional processor (without the cnd:noEmit escape)
  // strips the "#if !JARIBU_MULTI" / "#endif" directive lines while keeping the body unconditional
  // (task 134 finding M1, reproduced and confirmed fixed by the escape during task 135) — so their
  // literal presence post-generation is the actual proof, not TWA0008 staying quiet (TWA0008 only
  // flags tokens OUTSIDE the escaped region).
  public static readonly string[] JaribuMultiGuardLines =
  [
    "#if !JARIBU_MULTI",
    "return await TimeWarp.Jaribu.TestRunner.RunAllTests();",
    "#endif",
  ];
}
