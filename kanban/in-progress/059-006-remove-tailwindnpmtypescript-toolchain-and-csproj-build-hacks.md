# Remove Tailwind/npm/TypeScript toolchain + csproj build hacks

Parent: 059. **Last** — run only once nothing references Tailwind output (after 059-005).

Aligns with the standing project decision to drop the Tailwind/npm frontend toolchain
(see auto-memory `dropping-tailwind-npm-frontend`).

## Progress (2026-06-22) — Tailwind removed; TypeScript deferred

DONE (Tailwind): deleted `tailwind.config.js`, `styles/input.css` (+ `styles/`),
`wwwroot/css/site.css`; removed Tailwind devDeps + `css:build`/`tailwind-watch` scripts from
`package.json`; removed the `css:build` exec + `UpToDateCheckBuilt` Tailwind items from
`web-spa.csproj` (renamed `Tailwind` target → `WebSpaBuildTools`); swapped the `site.css`
link in `App.razor` for `tokens.css` + `app.css`; de-Tailwinded the error UI + body. Project
builds clean.

STILL TODO:
- [ ] TypeScript pipeline (`Microsoft.TypeScript.MSBuild`, `tsconfig.json`, `source/*.ts`,
      eslint/prettier, the `WebSpaBuildTools` target) — left intact; remove if TS is being
      dropped, else keep. (Does NOT affect CSS — that's why it was deferred.)
- [ ] csproj dummy-CSS targets (`CreateDummyFluentUICSS`, `CreateDummyQuickGridCSS`) — v4
      scoped-bundle hacks; re-evaluate under v5 (coordinate w/ 059-004 fluent.css item).
- [ ] Repo scripts `RunTailwind.ps1` / `RunNpmInstall.ps1` / `NpmOutdated.ps1` (under
      `TimeWarp.Architecture/`) — part of 059-007 template work.

## Tasks

### Delete (web-spa)
- [ ] `tailwind.config.js`
- [ ] `package.json`, `package-lock.json`, `node_modules/`
- [ ] `styles/input.css` (and `styles/` if empty)
- [ ] `.eslintrc.js`, `.prettierrc.json`, `.prettierignore`, `tsconfig.json`, `source/*.ts`
      (the TypeScript sources compiled by `Microsoft.TypeScript.MSBuild`) — confirm no TS is
      still needed; if JS interop is required, keep plain `.js` and drop the TS pipeline.
- [ ] generated `wwwroot/css/site.css`

### Edit `web-spa.csproj`
- [ ] Remove `<Target Name="Tailwind">` (npm install/prettier/lint/build/css:build).
- [ ] Remove `Microsoft.TypeScript.MSBuild` PackageReference + `<TypeScriptInputs>` +
      `TypeScriptCompileBlocked` / `SkipWebSpaBuildTools` plumbing.
- [ ] Remove `<UpToDateCheckBuilt Include="styles/input.css" />` + `tailwind.config.js` items.
- [ ] Remove `CreateDummyFluentUICSS` + `CreateDummyQuickGridCSS` targets (v4 scoped-bundle
      hacks — confirm unneeded under v5 first; coordinate with 059-003).
- [ ] Keep `CopyScopedCss` only if still needed; review.

### Edit `web-server/components/App.razor`
- [ ] Remove `<link href="css/site.css">` (replaced by `tokens.css` in 059-001).
- [ ] Remove/replace `<link href="css/fluent.css">` per 059-003.

### Repo scripts
- [ ] Delete `RunTailwind.ps1`, `RunNpmInstall.ps1`, `NpmOutdated.ps1` (and any
      `TimeWarp.Architecture/` copies) and references in docs/CLAUDE.md / dev-cli.

## Done when
- [ ] `git grep -i tailwind` and `git grep -i "npm "` return nothing meaningful in web-spa.
- [ ] Clean build with no node/npm present in the environment.
