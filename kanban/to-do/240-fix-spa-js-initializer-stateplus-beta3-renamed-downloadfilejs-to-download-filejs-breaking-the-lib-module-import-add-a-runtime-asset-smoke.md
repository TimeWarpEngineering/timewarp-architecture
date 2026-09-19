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

- [ ] Import + d.ts + comment updated; TS emit regenerated
- [ ] Runtime asset smoke test (initializer import graph → 200 + JS content type)
- [ ] Template-smoke asset check or documented follow-up
- [ ] `dev build` 0/0; SPA + web-server suites green; `ganda repo audit` clean; `dev template-smoke` passes
- [ ] Results and How to validate (console clean on both origins)

## Session

- Created: cockpit (2026-09-19)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Blocks the 2.0.0-beta.20 cut (source is already at beta.20; do not release until this lands).
- Files: `web-spa/source/web.spa.lib.module.ts`, `web-spa/source/types/download-file.d.ts`,
  `web-spa/java-script-interop-constants.cs`, tests under `tests/container-apps/web/web-server-integration-tests/`.
- Package evidence: `~/.nuget/packages/timewarp.state.plus/12.0.0-beta.3/…/staticwebassets/js/download-file.js`
  vs beta.1 `staticwebassets/js/downloadFile.js`.
- Prior: 116 (TS compile before SWA discovery), 200 (initializer must be in host list), 237 (State beta.3).

## Results

_Pending._

### How to validate

_Pending._
