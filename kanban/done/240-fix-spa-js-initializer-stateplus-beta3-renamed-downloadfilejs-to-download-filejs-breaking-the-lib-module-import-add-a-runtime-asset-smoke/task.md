# Fix SPA JS initializer: State.Plus beta.3 renamed downloadFile.js to download-file.js, breaking the lib module import; add a runtime asset smoke

## Description

Live after task 237 (State 12.0.0-beta.3) on `https://arch.timewarp.work` (Steve, 2026-09-19):

```
GET /_content/TimeWarp.State.Plus/js/downloadFile.js 404
Uncaught (in promise) TypeError: Failed to fetch dynamically imported module: /js/web.spa.<fp>.lib.module.js
```

Root cause: `web-spa/source/web.spa.lib.module.ts:22` statically imports
`/_content/TimeWarp.State.Plus/js/downloadFile.js`. TimeWarp.State.Plus **12.0.0-beta.3** ships
the static web asset as `staticwebassets/js/download-file.js` (kebab-case rename, timewarp-state
commit 746b12a0 "Rename all source files to kebab-case", after the beta.1 tag; beta.1 shipped
`downloadFile.js`). A failed static import rejects the whole module, so the Blazor JS
initializer never runs: `window.Spa` is never assigned (Counter interop breaks), and
`window.downloadFileFromStream` is never attached. Passkeys still work because
`web-authn.js` is imported on demand from C#, not through the initializer.

Template smoke and the SPA test suites only build and prerender; nothing executes the browser
module graph, so 237 could not see it. The rename is undocumented in the State release notes
(cockpit is adding a note to the v12.0.0-beta.3 GitHub release).

## Requirements

1. Fix the import: `web.spa.lib.module.ts` → `/_content/TimeWarp.State.Plus/js/download-file.js`;
   update `source/types/download-file.d.ts` (ambient module declaration) and the comment in
   `java-script-interop-constants.cs` (`wwwroot/js/downloadFile.js`). Rebuild the TS emit
   (`wwwroot/js/web.spa.lib.module.js` is gitignored emit; verify the build target regenerates it).
2. **Runtime asset smoke** so a renamed or missing static web asset fails CI, not the tester:
   in `web-server-integration-tests` (test host serves static web assets), fetch
   `/web-server.modules.json` (or the `Blazor-Web-Initializers` list — read how task 200 asserts the
   initializer is present), then for each initializer module fetch it and **parse its static
   `import` specifiers** (regex on `^import .* from "([^"]+)"` and `^import "([^"]+)"`), resolve
   root-relative ones, and assert each returns 200 with a JavaScript content type. Recurse one
   level (the `_content/TimeWarp.State/js/*.js` imports). This catches `_content` renames in
   any TimeWarp package.
3. If cheap: extend `dev template-smoke` with the same asset check against the generated app's
   built wwwroot + staticwebassets manifest (no server needed: resolve `_content/<pkg>/...` to the
   package's `staticwebassets/` folder in the NuGet cache). Otherwise note it as a follow-up.
4. Verify live: restart `dev run`, `https://localhost:63611` and `https://arch.timewarp.work` load
   with no console errors; Counter page interop works; a file download (if any page uses
   `downloadFileFromStream`) works.

## Checklist

- [x] Import + d.ts + comment updated; TS emit regenerated
- [x] Runtime asset smoke test (initializer import graph → 200 + JS content type)
- [x] Template-smoke asset check or documented follow-up
- [x] `dev build` 0/0; SPA + web-server suites green; `dev template-smoke` passes
- [x] Results and How to validate (in-proc host graph 200; live origins need a recycle after merge)
- [x] Implementation review disposition (`clean`, 1 round, general only)

## Session

- Created: cockpit (2026-09-19)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok (2026-09-19) — kebab-case `_content` imports, runtime import-graph smoke, template-smoke NuGet-cache check
- Review oracle: Grok session `01a0b917-dc84-7c33-9376-768e42d7aa6a` (2026-09-19) — effort 1, general only; disposition `clean`

## Notes

- Blocks the 2.0.0-beta.20 cut (source is already at beta.20; do not release until this lands).
- Files: `web-spa/source/web.spa.lib.module.ts`, `web-spa/source/types/download-file.d.ts`,
  `web-spa/java-script-interop-constants.cs`, tests under `tests/container-apps/web/web-server-integration-tests/`.
- Package evidence: `~/.nuget/packages/timewarp.state.plus/12.0.0-beta.3/…/staticwebassets/js/download-file.js`
  vs beta.1 `staticwebassets/js/downloadFile.js`.
- Prior: 116 (TS compile before SWA discovery), 200 (initializer must be in host list), 237 (State beta.3).
- Same beta.3 kebab-case drop also renamed TimeWarp.State `Logger.js` → `logger.js`,
  `Constants.js` → `constants.js`, `TimeWarpState.js` → `timewarp-state.js`. Those
  specifiers were updated too; a download-file-only fix would still 404 the initializer
  on a case-sensitive host.

## Results

TimeWarp.State.Plus **12.0.0-beta.3** ships `download-file.js`. The SPA JS initializer
statically imported the beta.1 name `downloadFile.js`, so the browser 404'd that module
and rejected `web.spa.*.lib.module.js` entirely (`window.Spa` never assigned). The same
State kebab-case rename also applied to `logger.js` / `constants.js` / `timewarp-state.js`.

**What landed**

- SPA initializer and Counter import kebab-case `_content` specifiers; ambient `.d.ts`
  module names and the `JavaScriptInteropConstants` path comment match.
- TypeScript emit regenerated (`wwwroot/js/web.spa.lib.module.js` now imports
  `download-file.js`, `logger.js`, `constants.js`).
- Runtime smoke in `web-server-integration-tests`: load `/web-server.modules.json`
  (HTML `<!--Blazor-Web-Initializers:base64-->` fallback), GET each initializer, parse
  static `import` specifiers, assert root-relative targets return 200 + a JavaScript
  content type, recurse one level.
- Host-free emit assertion that the compiled initializer contains kebab-case paths
  (ordinal; Shouldly's default compare is case-insensitive).
- `dev template-smoke` after each generated-app build resolves `/_content/<pkg>/...`
  to `{NuGet cache}/<pkg>/<version>/staticwebassets/...` (3 specifiers per matrix entry).

**Files**

- `source/container-apps/web/projects/web-spa/source/web.spa.lib.module.ts`
- `source/container-apps/web/projects/web-spa/source/features/counter.ts`
- `source/container-apps/web/projects/web-spa/source/types/*.d.ts`
- `source/container-apps/web/projects/web-spa/java-script-interop-constants.cs`
- `tests/container-apps/web/web-server-integration-tests/js-initializer-import-graph-tests.cs`
- `tools/dev-cli/services/template-smoke-initializer-assets.cs`
- `tools/dev-cli/endpoints/template-smoke-command.cs`
- `tools/dev-cli/services/template-smoke-harness.cs` (Design region)

**Test outcomes**

- `dotnet run tools/dev-cli/dev.cs -- build` — 0/0
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release` — 241 passed, 1 skipped (`RunForever`)
- `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release` — 38 passed, 1 skipped (weather quarantine)
- `--filter-class JsInitializer` — 3/3 passed (emit + HTTP graph + task-200 manifest)
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` — SUCCEEDED; all three matrix entries printed `Initializer import graph OK (3 _content specifier(s) resolved …)`
- In-proc web host GET `/_content/TimeWarp.State.Plus/js/download-file.js` → 200 `text/javascript`

**Live origins:** `https://arch.timewarp.work` still serves the pre-fix initializer
(`import "/_content/TimeWarp.State.Plus/js/downloadFile.js"`; that URL is 404,
`download-file.js` is 200). Recycle `dev run` / the public share after merge.

**Audit:** `ganda repo audit` still reports origin/master pre-existing failures
(26 runfiles tracked as `100644` without +x, `kanban/done/238-…/run-rank-experiment.cs`
shebang, vscode peacock, memsearch hooks). `--fix` can clear them; those diffs are
out of scope for this initializer fix.

### Review disposition

- **Rounds:** 1 · **Effort:** 1 · **Roster:** general
- **Counts (final):** bug 0 / suggestion 0 / nit 0 (all open=0, fixed=0, wontfix=0)
- **Disposition:** `clean` — no findings raised
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`
- **Wontfix / escalations:** none. Fix loop stayed on this task id (none needed).

### How to validate

**Smoke**

1. `dotnet run tools/dev-cli/dev.cs -- build` (or `./bin/dev build` after self-install).
2. `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class JsInitializer`
3. After a recycle of `dev run`, open `https://localhost:63611` and `https://arch.timewarp.work` with the browser console. Visit Counter and increment.
4. Optional: `curl -sI https://localhost:63611/_content/TimeWarp.State.Plus/js/download-file.js` and the same URL on `arch.timewarp.work`.

**Expect**

- Build 0/0. JsInitializer filter: 3 passed (emit contains kebab-case `_content` paths; HTTP graph 200 + JS content type).
- Browser console has no `Failed to fetch dynamically imported module` and no 404 for `downloadFile.js`.
- Counter increment runs (`Spa.Counter` / `timewarp-state.js` dispatch). Passkeys still use on-demand `web-authn.js`.
- `download-file.js` returns HTTP 200 and `text/javascript`. `downloadFile.js` may 404; that is the old name.

**Automated gate**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release
# expect: 241 passed, 1 skipped (RunForever)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release
# expect: 38 passed, 1 skipped (weather quarantine)

dotnet run tools/dev-cli/dev.cs -- template-smoke
# expect: SUCCEEDED, and "Initializer import graph OK (3 _content specifier(s) resolved …)" once per matrix entry
```

**Depends on:** web-server-integration HTTP tests boot HostGraph on `https://localhost:7000` (and Api on `7255` when the `api` flag is on). Ports must be free.

**Not in scope:** restarting the operator's already-running `dcp` share from this session; WebAuthn hardware; a real `downloadFileFromStream` UI (no page in this template exercises it beyond attaching `window.downloadFileFromStream`).
