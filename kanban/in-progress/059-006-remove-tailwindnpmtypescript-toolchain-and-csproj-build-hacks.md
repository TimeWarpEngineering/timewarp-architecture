# Remove Tailwind/npm/TypeScript toolchain + csproj build hacks

Parent: 059. **Last** — run only once nothing references Tailwind output (after 059-005).

Aligns with the standing project decision to drop the Tailwind/npm frontend toolchain
(see auto-memory `dropping-tailwind-npm-frontend`).

**Decision (2026-06-24): the TypeScript pipeline is KEPT.** The `source/*.ts` → `wwwroot/js`
compile (`Microsoft.TypeScript.MSBuild`, node-free) is required for the JS-interop demo
(`window.Spa.Counter`) and the Blazor JS initializer. Only the Tailwind + npm lint/format bits
are dropped; TS-removal items are struck through below.

## Tasks

### Delete (web-spa)
- [x] `tailwind.config.js`
- [x] `styles/input.css` (and `styles/`)
- [x] generated `wwwroot/css/site.css`
- [ ] `package.json`, `package-lock.json`, `node_modules/`
- [ ] `.eslintrc.js`, `.prettierrc.json`, `.prettierignore` _(npm lint/format; `tsconfig.json` + `source/*.ts` KEPT — TS retained)_

### Edit `web-spa.csproj`
- [x] Remove `<Target Name="Tailwind">` (npm install/prettier/lint/build/css:build)
- [x] Remove `CreateDummyFluentUICSS` + `CreateDummyQuickGridCSS` targets
- [x] Remove `CopyScopedCss` target
- [x] Remove `<UpToDateCheckBuilt>` (input.css) + `tailwind.config.js` items
- [x] ~~Remove `Microsoft.TypeScript.MSBuild` + `<TypeScriptInputs>` + `TypeScriptCompileBlocked`~~ — `TypeScriptCompileBlocked` removed; package + inputs KEPT (TS retained)
- [ ] Drop the npm steps in the `WebSpaBuildTools` target (and the target itself if nothing else uses it)

### Edit `web-server/components/App.razor`
- [x] Remove `<link href="css/site.css">`
- [x] Remove `<link href="css/fluent.css">`

### Repo scripts
- [ ] Delete `RunTailwind.ps1` / `RunNpmInstall.ps1` / `NpmOutdated.ps1` (under `TimeWarp.Architecture/`) — may fold into 059-007

## Done when
- [ ] `git grep -i tailwind` and `git grep -i "npm "` return nothing meaningful in web-spa
- [ ] Clean build with no node/npm present in the environment
