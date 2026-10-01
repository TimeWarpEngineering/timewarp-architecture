# Update to .NET 11 and adopt AddDotnetProject

## Description

Move the template from .NET 10 to **.NET 11**. Then adopt Aspire's `AddDotnetProject()`
(`Aspire.Hosting.Dotnet`), whose multi-threaded MSBuild needs the .NET 11 SDK. Steve decided this
on 2026-10-01. .NET 11 GA is roughly a month out, so **do not start until a .NET 11 SDK suitable
for the template is available.** Prefer GA. If Steve says to go earlier, use the latest RC and
record that it is prerelease.

## Requirements

1. **SDK and TFM.**
   - Bump the root `global.json` SDK pin.
   - **Mirror the pin in every project-local `global.json`.** The family `JARIBU_MULTI`
     aggregators under `tests/container-apps/<family>/<family>-jaribu-tests/` must match the
     root (timewarp-jaribu#20).
   - Move `net10.0` to `net11.0` everywhere, including runfile `#:` directives, the
     template.json / `.template.config`, and CI workflows that install an SDK.
2. **Packages.** Move ASP.NET Core / EF Core / `Microsoft.Extensions.*` CPM pins to their 11.x
   versions, and any analyzer or SDK-coupled packages.
   - Pins move forward only; first-party TimeWarp packages are never pinned backward. If a
     TimeWarp package lacks .NET 11 support, fix it upstream rather than pin back.
   - Check the Blazor WASM workload and the WASM-specific packages.
3. **Aspire `AddDotnetProject()`.** Adopt `Aspire.Hosting.Dotnet` once it is out of prerelease;
   if it is still prerelease, record that and ask Steve.
   - Rewrite the `AddProject<Projects.*>` calls.
   - Adjust the `Projects.*` typed `ProjectReference` wiring.
   - Reconcile TWA0007, the analyzer that checks resource names against `ServiceNames`, with the
     new API.
   - Run the CLI skill `aspire-project-v2-migration` if it helps, but review its output; do not
     accept it blindly.
4. **Fix what breaks.**
   - Every new .NET 11 analyzer warning (warnings are errors): fix it, never mass-suppress.
   - Obsoletions, breaking changes, and Jaribu / Shouldly / FluentUI / TimeWarp.State
     compatibility.
5. **Release.** The template and packages ship on .NET 11, so `dev check-version` applies. Bump
   the version and pins in the same commit (policy 124).

## Checklist

- [ ] SDK pin bumped; all project-local `global.json` mirror it; `net11.0` everywhere (projects,
      runfiles, template config, CI)
- [ ] Framework/EF/Extensions pins on 11.x; no backward pins; WASM workload OK
- [ ] `AddDotnetProject()` adopted (or a recorded reason plus a question to Steve if still
      prerelease); TWA0007 reconciled
- [ ] New .NET 11 warnings fixed (no blanket suppression)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`,
      `dev check-version` (version + pins bumped together)
- [ ] Do **not** start an AppHost; record the run check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 22930 (2026-10-01)

## Notes

- **Blocked until a .NET 11 SDK is available** (GA expected about a month after 2026-10-01).
  Do not dispatch before then unless Steve says so.
- Origin: task 262's evaluate-only recommendation for `AddDotnetProject()`.
- Memory discipline: a full-solution TFM bump is a heavy build. Run serially and call
  `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*
