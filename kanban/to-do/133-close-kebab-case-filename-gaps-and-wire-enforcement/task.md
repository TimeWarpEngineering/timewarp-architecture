# Close kebab-case filename gaps and wire enforcement

## Description

Close remaining **kebab-case path/filename** debt in this template repo and make enforcement
real (docs + analyzer wiring), without fighting Blazor’s required PascalCase `.razor` names.

**Audit report (2026-07-29):**
`.agent/workspace/2026-07-29T03-17-10_kebab-case-filename-violations.md`

### What is already good

| Area | Status |
|------|--------|
| Axis-1 `source/container-apps/{web,api,grpc}/{features,platform}/` | Fully kebab |
| `source/foundation/`, analyzers `.cs` | Fully kebab |
| All `source/**/*.cs` **except** residual SPA pure-`.cs` outliers | Clean (264/264 excl. web-spa in audit) |
| `tests/.../web-contracts-tests/` | Kebab model to copy |
| SPA folders + TimeWarp.State partials (`application-state.close-modal.cs`) | Kebab |

### Explicit exception (do **not** rename)

**`.razor` (and matching `.razor.cs` / `.razor.css`) stay PascalCase** — Blazor requires the
component file name to match the type name.

Evidence:

- **Analyzer allowlist:** `TimeWarp.SourceGenerators` `FileNameRuleAnalyzer` default exceptions
  include `*.razor.cs` (“Razor component code-behind files must match their `.razor` file names”).
  Source:
  `timewarp-source-generators/.../file-name-rule-analyzer.cs`
- **Ganda epic 102 / task 103:** “`.razor` files must use PascalCase (Blazor requirement); all
  other files/dirs use kebab-case”
- **TWA dogfood note:** task 126 findings — SPA grammar carve-out; `PasskeysPage.razor` is ordinary
  Blazor Pascal naming, not axis-1 `-layer` grammar

### Where the analyzer lives (past work)

| Item | Detail |
|------|--------|
| Package | **`TimeWarp.SourceGenerators`** (CPM pin here: `1.0.0-beta.8`) |
| Type | `FileNameRuleAnalyzer` (`IIncrementalGenerator` reporting diagnostics) |
| Diagnostic id | **`TW0001`** (“File name should use kebab-case”) — package family **`TW*`**, **not** Architecture **`TWA*`** |
| Default | `isEnabledByDefault: false`, severity **Info** |
| Scope | **`.cs` only** — not directories, not `.md` / `.csproj` / `.razor` |
| Pattern | `^[a-z][a-z0-9]*(?:-[a-z0-9]+)*\.cs$` (simple single-stem kebab — **does not** accept multi-dot partials like `application-state.close-modal.cs`) |
| Exceptions | `*.g.cs`, `*.Generated.cs`, `*.razor.cs`, `*AssemblyInfo.cs`, AnalyzerReleases, etc.; plus `.editorconfig` `dotnet_diagnostic.TW0001.excluded_files` |
| Origin task | timewarp-source-generators task **011** |
| Prefix SSOT | source-generators task **020** (done): **keep `TW*`**, docs-only — no rename; do **not** use `TWA001` / `TWG` for this package |
| ADR | timewarp-flow **ADR-0013** (Adopt kebab-case file naming) — calls for this analyzer |

**Wiring gap in this monorepo:** `TimeWarp.SourceGenerators` is pinned in CPM and referenced by
`tests/common/timewarp-testing` (and historically listed in package hygiene tasks) but **not** used
as a repo-wide kebab gate. Ganda task 144 deferred wiring + non-`.cs` checks; Ganda
`file-naming.md` may still say the wrong id (`TWA001`) until that repo is updated.

**Prefix status (resolved upstream):** Shipped ids are **`TW0001`–`TW0006`**. Architecture owns
**`TWA*`** — no real Roslyn collision. External/historical docs that said `TWA001` for the kebab
rule were wrong; source-generators docs now state SSOT explicitly (task 020).

### Remaining debt (after razor exception)

1. **Integration tests (high)** — Pascal `Features/` dirs + `*_Tests.cs` / `GlobalUsings.cs` in
   web-server, web-spa, api-server integration projects (contracts tests already kebab).
2. **SPA pure `.cs` outside code-behind (low)** — e.g. `BadgeStatus.cs` → `badge-status.cs` if the
   type is not a razor-paired component file.
3. **Documentation (medium)** — `HowTo*.md`, Title Case conceptual docs, underscore hybrids.
4. **Assets / `Properties/` (low)** — logos Pascal; ASP.NET `Properties/` host folders — allowlist.
5. **Docs/skills in this repo** — state razor exception + analyzer package; ADR-0013 not copied into
   TWA `documentation/` tree (lives in timewarp-flow).

## Requirements

- Treat **`.razor` / `.razor.cs` / `.razor.css` as first-class exceptions** in TWA docs and skills
  (`AGENTS.md`, `tw-csharp`, feature-placement if needed) — not as violations.
- Inventory remaining non-razor, non-allowlist violations and rename or allowlist them.
- Migrate legacy integration-test path casing to match `web-contracts-tests` kebab layout.
- Wire or document kebab enforcement:
  - Prefer enabling/fixing **`TimeWarp.SourceGenerators` `FileNameRuleAnalyzer`**, with a plan for
    **multi-dot partial** filenames used heavily by TimeWarp.State in this template.
  - Resolve **TWA001 id collision** with Architecture `TWA0001` (rename SourceGenerators id or
    accept dual meaning with loud docs — prefer rename upstream).
  - Optionally add a `ganda repo audit` check for dirs / non-`.cs` later (Ganda 144 deferred this).
- Analyzer tests: valid kebab, multi-dot partials if pattern extended, razor.cs excluded, generated
  files excluded.

## Checklist

### Docs / policy (this repo)

- [ ] Document razor + ASP.NET host exceptions in `AGENTS.md` and `tw-csharp` (or in-repo
      `documentation/developer/standards/file-naming.md` mirroring Ganda)
- [ ] Point to ADR-0013 (timewarp-flow) and SourceGenerators `FileNameRuleAnalyzer`
- [ ] Update analysis report disposition: SPA razor **not debt**

### Remediations

- [ ] Rename remaining pure `.cs` SPA outliers (e.g. `BadgeStatus.cs`) if not code-behind
- [ ] Kebab-migrate `web-server-integration-tests` paths/files
- [ ] Kebab-migrate `api-server-integration-tests` paths/files
- [ ] Kebab-migrate `web-spa-integration-tests` paths/files
- [ ] Kebab-migrate `aspire-tests` leftovers (`GlobalUsings.cs`, `IntegrationTest1.cs`)
- [ ] Optional pass: documentation basenames (`HowTo*` → `how-to-*`, etc.)
- [ ] Optional pass: template logo asset names

### Enforcement

- [ ] Confirm which projects reference `TimeWarp.SourceGenerators` today
- [ ] Upstream (timewarp-source-generators): extend kebab regex for **multi-dot partials**
      (`name.part.cs`) used by this template’s state actions
- [ ] Upstream: rename diagnostic id off Architecture collision (`TWG…` preferred)
- [ ] Enable rule at sensible severity for product/test `.cs` after renames (or gate via
      `.editorconfig` per tree)
- [ ] Consider `ganda repo audit` check for directory basenames / docs (follow Ganda 144 notes)

### Verify

- [ ] `dev build` 0/0
- [ ] Affected `dev test` / Fixie projects green
- [ ] Re-run kebab audit script; only allowlisted exceptions remain

## Notes

### Cross-repo references

| Repo | Artifact |
|------|----------|
| timewarp-source-generators | `file-name-rule-analyzer.cs`, task 011 |
| timewarp-flow | ADR-0013 kebab-case adoption |
| timewarp-ganda | `documentation/developer/standards/file-naming.md`; epic 102 razor exception; task 144 audit vs analyzer limits |
| timewarp-architecture | This task; audit report under `.agent/workspace/` |

### Allowlist candidates (do not thrash without decision)

- `Properties/`, `launchSettings.json`, `appsettings.<Environment>.json`
- `AnalyzerReleases.Shipped.md` / `Unshipped.md`
- `_Imports.razor`, `App.razor`
- MSBuild well-known props/targets
- Static brand assets under `wwwroot/images/` and `timewarp-templates/assets/` (optional rename)

### Related analyzers (different jobs)

| Id / analyzer | Job |
|---------------|-----|
| SourceGenerators **TWA001** `FileNameRuleAnalyzer` | Generic `.cs` kebab basename |
| Architecture **TWA0001** | Partial class primary/secondary **declaration shape** (accepts Pascal *or* kebab file names today) |
| Architecture **TWA0015/0016** | Axis-1 feature **function/layer** grammar under family trees |

## Session

- Created: Grok analysis + task open (2026-07-29) — audit report + SourceGenerators/Ganda research
