# web-server JS initializer manifest must include Web.Spa so window.Spa cannot go missing

## Description

Login `/Login` (and Settings add-passkey) call `IJSRuntime` string identifiers
`Spa.WebAuthn.CreateCredential` / `Spa.WebAuthn.GetCredential`. Blazor resolves those as
`window.Spa.WebAuthn.*`. `window.Spa` is assigned only by the Web.Spa JS initializer
(`source/container-apps/web/projects/web-spa/source/web.spa.lib.module.ts` →
`wwwroot/js/web.spa.lib.module.js`), and Blazor only loads that file if it appears in the
page's `Blazor-Web-Initializers` list — which is web-server's generated
`web-server.modules.json` / `obj/.../jsmodules/jsmodules.build.manifest.json`.

On 2026-08-25 the public share `https://arch.timewarp.work/Login` threw:

```text
Could not find 'Spa.WebAuthn.CreateCredential' ('Spa' was undefined)
```

on both **Sign in with a passkey** and **Create account**. The ceremony C# and WebAuthn
RP-ID path never ran. A later Aspire recycle regenerated the host initializer list, the
page started loading `js/web.spa.hpseta122u.lib.module.js`, `window.Spa` existed, and
Login worked again. This has confused the operator more than once.

This is a **host build / static-web-asset** hole, not a passkey-protocol bug.

## Why the list goes stale

- SDK tags JS initializers with `RelativePathFilter="**/$(PackageId).lib.module.js"`
  (`PackageId` / `AssemblyName` = `Web.Spa`). web-server then aggregates referenced
  `JSLibraryModule` assets into its modules.json.
- `wwwroot/js/` is gitignored TypeScript output (`ad19d511`, 2026-07-28). After that
  commit the committed copies of `web.spa.lib.module.js` / `spa.js` / `web-authn.js`
  were deleted; discovery only sees files that exist on disk at host SWA resolution.
- Task **116** ordered TypeScript **inside** web-spa (`PrepareForBuildDependsOn`) so
  `dotnet build -t:Rebuild` of web-spa itself no longer races StaticWebAssets. It does
  **not** force web-server to rebuild its initializer manifest when those generated JS
  files appear or change.
- Incremental `dotnet build` / Aspire start can therefore emit a running web-server
  whose modules.json lists FluentUI + TimeWarp.State + HotReload and **omits Web.Spa**.
- Task **104-016** (`057dae7a`) removed the explicit Passwordless `<script type="module">`
  from `App.razor`. Login now depends entirely on that initializer list. A missed Spa
  module is a hard fail on the product CTA.

Incident timestamps (operator machine, 2026-08-25 +0700):

| Artifact | Time | Note |
|----------|------|------|
| `wwwroot/js/web.spa.lib.module.js` | 10:04 | TS emit present on disk |
| `web-server.dll` | 10:04 | host binary rebuilt |
| Login HTML `Blazor-Web-Initializers` | 11:24 | FluentUI, TimeWarp.State, HotReload — **no Spa** |
| `jsmodules.build.manifest.json` | 11:26 | regenerated on Aspire recycle; includes `js/web.spa.*.lib.module.js` |

The 10:04 host build did not refresh the initializer list. The 11:26 recycle did. That
is the flip the operator saw as "it is working now."

## Requirements

- After a successful **web-server** build (incremental, `Rebuild`, and Aspire project
  start), the host JS initializer list **must** include the Web.Spa initializer
  (`web.spa*.lib.module.js`, fingerprinted name ok). A missed Spa module is a failed
  build or a failed automated test — not a runtime surprise on `/Login`.
- Keep task 116's intra-web-spa TS-before-SWA ordering. This task is the **host**
  side: web-server's modules.json must be a hard function of web-spa's generated
  initializer, not an incrementally skippable side effect.
- Automated gate that fails when the host list omits Web.Spa (read
  `jsmodules.build.manifest.json` / `web-server.modules.json` after build, or an
  equivalent MSBuild/test assertion). `dev build` 0/0 is not enough by itself.
- Login / Settings add-passkey must not be able to throw `'Spa' was undefined` because
  the host omitted the initializer. Prefer also moving the ceremony off
  `Spa.WebAuthn.*` string identifiers onto on-demand `IJSRuntime` `import()` of
  `./js/features/web-authn.js` (named exports) so the passkey path does not require
  `window.Spa` at all. Counter JS interop (`Spa.Counter.*`) may keep the global.

## Checklist

- [x] Host build always tags/emits Web.Spa in web-server's JS initializer list (incremental + Rebuild + Aspire start)
- [x] Automated gate: fail if `web-server.modules.json` / jsmodules manifest omits `web.spa*.lib.module.js`
- [x] Passkey ceremony does not depend on `window.Spa` (on-demand import of `web-authn.js`) — or prove the host gate makes that impossible and document why the global remains
- [x] `dev build` 0/0; tests for the gate green
- [x] Design regions on touched files reconciled (`web.spa.lib.module.ts`, `passkey-ceremony-client.cs`, web-spa csproj if the host coupling is declared there)

## Notes

- Repro symptom: `/Login` both buttons →
  `Could not find 'Spa.WebAuthn.CreateCredential' ('Spa' was undefined)`.
- Call sites today: `passkey-ceremony-client.cs`, `credentials-state.add-passkey.cs`.
- Related: task **116** (web-spa TS vs SWA Rebuild), **104-016** (Passwordless removed;
  Login uses `Spa.WebAuthn.*`), commit `ad19d511` (gitignore + delete committed
  `wwwroot/js`).
- Not in scope: WebAuthn RP-ID / `AllowedRpIds` (the ceremony never started while `Spa`
  was undefined). Playwright-only RP-ID errors after the initializer returned are a
  different path.

## Session

- Created: 411944 (2026-08-25)
- Diagnosed on `https://arch.timewarp.work/Login` (Aspire Development share, task 112
  ingress); first HTML capture lacked the Spa initializer; post-recycle HTML included
  `js/web.spa.hpseta122u.lib.module.js` and `window.Spa.WebAuthn.CreateCredential` was a
  function.
- Cockpit: Grok launch (2026-08-31) — leftover worktree claimed, FF to origin/master,
  moved in-progress, dispatch `ganda task work`
- Implementation: Grok implement oracle (2026-08-31) — host Content re-glob + MSBuild
  assertion + on-demand web-authn import; solution Release 0/0; gate tests green
- Review: Grok review oracle (2026-08-31) — effort 1 general, 2 rounds, disposition clean

## Results

The `/Login` `'Spa' was undefined` failure had two layers. Passkey C# no longer
walks `window.Spa`: `PasskeyCeremonyClient` and Settings add-passkey call
`WebAuthnJsModule`, which `import()`s `./js/features/web-authn.js` and invokes the
named `CreateCredential` / `GetCredential` exports. Counter JS interop
(`Spa.Counter.*`) still needs the initializer.

The host list is no longer an incrementally skippable side effect of evaluation-time
`wwwroot/**` Content. After TypeScript emit (task 116), web-spa re-globs
`wwwroot/js/**` into Content so the same build's SWA discovery tags
`web.spa.lib.module.js`. web-server fails the build (and publish) if
`_ExistingBuildJSModules` / `_ExistingPublishJSModules` omit `web.spa*.lib.module.js`,
and Fast Up-to-date Check watches the emit path. A Jaribu test reads the generated
`jsmodules.build.manifest.json` after build.

Verified: Release web-server build writes
`js/web.spa.foa7bin14p.lib.module.js` into the host manifest. Deleting gitignored
`wwwroot/js` and incrementally rebuilding web-server re-emits TypeScript and still
lists Web.Spa. `dotnet build timewarp-architecture.slnx -c Release` is 0/0.

### How to validate

**Smoke**

1. `dotnet build source/container-apps/web/projects/web-server/web-server.csproj -c Release`
2. `cat source/container-apps/web/projects/web-server/obj/Release/net10.0/jsmodules/jsmodules.build.manifest.json`
3. `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class HostBuild_Given_`
4. `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class Import_Given_`
5. Optional product check: open `/Login` and use Sign in with a passkey / Create account — must not throw `'Spa' was undefined` even if Counter JS interop is the only remaining `window.Spa` consumer.

**Expect**

- Host build succeeds (0/0). Omitting Web.Spa from the initializer list is a failed build, not a running host.
- Manifest JSON contains `js/web.spa*.lib.module.js` (fingerprinted name ok).
- `HostBuild_Given_` passes (manifest gate).
- `Import_Given_` passes: identifiers are `import` + `CreateCredential`/`GetCredential`, specifier is `./js/features/web-authn.js`, and the two C# call sites do not contain `Spa.WebAuthn`.
- Login passkey buttons invoke `web-authn.js` via module import; they do not resolve `window.Spa.WebAuthn`.

### Review disposition

**Outcome:** clean
**Rounds:** 2
**Effort:** 1 (general only)
**Roster:** general

Final counts (round 2):

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

Round 1 raised **M1** (nit): `WebAuthnJsModule` Design claimed Blazor import-map remapping, but `App.razor` has no `<ImportMap />`. Fixed on this task id — Design now cites `<base href="/" />` plus MapStaticAssets dual endpoints. Round 2 confirmed M1 fixed; no new findings. No sibling apply-review task.

Paths:

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/round-2/general.md`
- `review/round-2/merged.md`
- `review/disposition.md`
