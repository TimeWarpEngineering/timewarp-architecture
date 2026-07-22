# Fix template sourceName rewriting TimeWarp.Architecture package ids and unpublished TimeWarp.Identity reference

## Description

Found 2026-07-22 during the orchestrator's independent review of 114-002 (first template
generate+build in weeks). `dotnet new timewarp-architecture -n SmokeDefault` produces an app
whose restore fails with 54 NU1101s, ALL pre-existing (not 114-002):

1. Template `sourceName` is `TimeWarp.Architecture`, so the engine rewrites the PACKAGE IDS
   `TimeWarp.Architecture.Analyzers` / `.Attributes` / `.Generators` in csproj/CPM into
   `SmokeDefault.Analyzers` etc. — nonexistent packages. Broken since the analyzer-packages
   dual-mode (092) landed those references. Fix: exempt the package-reference literals from
   sourceName substitution (template.json replacement exclusions, or restructure the ids so
   substitution can't touch them), and add a template-output restore/build smoke to CI so this
   class of break can't ship silently again (JT's test-templates.yml is prior art).
2. `TimeWarp.Identity` package referenced by generated output but not yet published (104
   program library). Decide: publish, or template consumes it via foundationPackages-style
   dual-mode/source inclusion until publish.

## Checklist

- [ ] Exempt TimeWarp.Architecture.* package ids from sourceName substitution; regenerate + restore green
- [ ] Resolve TimeWarp.Identity availability for generated apps (publish or dual-mode)
- [ ] Template smoke (generate + restore + build, both postgres states) wired into CI
- [ ] Both flag states build 0/0 from generated output

## Notes

Discovered via 114-002 review closing its 'template smoke not run' gap; smoke scripts/logs in session scratchpad. TWA0015/16 note: generated apps get grammar msbuild guard immediately, but analyzer diagnostics only after TimeWarp.Architecture.Analyzers republishes (pins lag published — expected).
