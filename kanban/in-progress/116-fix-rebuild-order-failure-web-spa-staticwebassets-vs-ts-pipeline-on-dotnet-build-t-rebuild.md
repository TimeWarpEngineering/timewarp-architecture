# Fix rebuild-order failure web-spa StaticWebAssets vs TS pipeline on dotnet build -t Rebuild

## Description

`dotnet build -t:Rebuild` on any graph including web-spa intermittently/reliably fails:
Rebuild cleans wwwroot/js outputs (TS pipeline), and Microsoft.NET.Sdk.StaticWebAssets'
DefineStaticWebAssets then throws `No file exists for the asset ... wwwroot/js/features/counter.js`
before the TS compile re-emits it. Found 2026-07-22 during 114-002 review; retroactively
explains the 114-001 spike's 'intermittent -t:Rebuild 1 Error' mystery (previously suspected
output race — wrong). Pre-existing, unrelated to axis-1 work. Fix direction: ensure TS-compile
target runs before StaticWebAssets collection after clean (BeforeTargets ordering), or exclude
the generated js from Clean, or emit to obj/ and map as static web asset properly (relates to
the web-spa TS pipeline notes in memory/058-001 hardening).

## Checklist

- [ ] Reproduce deterministically (dotnet build -t:Rebuild web-server graph)
- [ ] Fix target ordering (or asset mapping) so Rebuild is reliable
- [ ] Verify: 3 consecutive -t:Rebuild runs green; note in web-spa csproj Design comments

## Notes

Captured log: StaticWebAssets.targets(706,5) InvalidOperationException on counter.js. Coordinates with 058-001 (test-host hardening) but is a build-graph issue, not test infra.
