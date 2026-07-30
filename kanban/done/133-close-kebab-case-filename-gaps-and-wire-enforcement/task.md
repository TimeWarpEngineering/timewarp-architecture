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
  - Prefer enabling/fixing **`TimeWarp.SourceGenerators` `FileNameRuleAnalyzer` (`TW0001`)**, with
    a plan for **multi-dot partial** filenames used heavily by TimeWarp.State in this template.
  - Prefix is settled (**`TW*`** keep — source-generators task 020); configure only
    `dotnet_diagnostic.TW0001.*`, never Architecture `TWA*`.
  - Optionally add a `ganda repo audit` check for dirs / non-`.cs` later (Ganda 144 deferred this).
- Analyzer tests: valid kebab, multi-dot partials if pattern extended, razor.cs excluded, generated
  files excluded.

## Checklist

### Docs / policy (this repo)

- [x] Document razor + ASP.NET host exceptions in `AGENTS.md` and in-repo
      `documentation/developer/standards/file-naming.md` (`tw-csharp` already expanded in flow)
- [x] Point to ADR-0013 (timewarp-flow) and SourceGenerators `FileNameRuleAnalyzer` / **TW0001**
- [x] SPA razor **not debt** (policy + audit disposition)

### Remediations

- [x] Rename remaining pure `.cs` SPA outliers (`BadgeStatus.cs` → `badge-status.cs`)
- [x] Kebab-migrate `web-server-integration-tests` paths/files
- [x] Kebab-migrate `api-server-integration-tests` paths/files
- [x] Kebab-migrate `web-spa-integration-tests` paths/files
- [x] Kebab-migrate `aspire-tests` leftovers (`global-usings.cs`, `integration-test1.cs`)
- [ ] Optional pass: documentation basenames (`HowTo*` → `how-to-*`) — **deferred** (not blocking)
- [ ] Optional pass: template logo asset names — **deferred** (allowlist)

### Enforcement

- [x] Confirm which projects reference `TimeWarp.SourceGenerators` today — CPM pin +
      `tests/common/timewarp-testing` only (not repo-wide product gate yet)
- [ ] Upstream (timewarp-source-generators): extend kebab regex for **multi-dot partials**
      (`name.part.cs`) — **blocked outside this repo**; ~40 SPA state partials would fail current pattern
- [x] ~~Upstream: rename diagnostic id~~ — **N/A** (task 020: keep **`TW*`**, docs SSOT; no rename)
- [ ] Enable **`TW0001`** severity — **blocked** until multi-dot pattern ships; documented in AGENTS
- [x] Ganda **task 188**: `kebab-path-names` — **done upstream** before this task finished (audit check shipped)
- [x] Ganda `file-naming.md` `TWA001` → **`TW0001`** (done on ganda `dev`)

### Verify

- [x] `dev build` 0/0 (after renames)
- [ ] Full `dev test` suite — not re-run end-to-end this session (build includes test projects compile)
- [x] High-debt Pascal test trees remigrated; residual = razor + allowlisted host/docs/assets

## Notes

### Implementation plan (executed 2026-07-29)

1. Policy docs in TWA (`AGENTS.md`, `documentation/developer/standards/file-naming.md`).
2. Kebab-migrate integration/aspire test basenames + dirs (`Features/` → `features/`, etc.).
3. Rename pure-`.cs` SPA outlier `badge-status.cs`.
4. Do **not** enable `TW0001` until SourceGenerators multi-dot support (task **021** there).
5. Ganda **188** (`kebab-path-names`) already shipped — not part of this implement wave.
6. Optional HowTo/logo renames deferred.

### Cross-repo references

| Repo | Artifact |
|------|----------|
| timewarp-source-generators | `file-name-rule-analyzer.cs`, task 011; task **020** (done) — **`TW*`** SSOT docs |
| timewarp-flow | ADR-0013; `tw-csharp` file-naming section |
| timewarp-ganda | `file-naming.md`; **task 188** kebab-path-names audit (**done**, check shipped) |
| timewarp-architecture | This task |

### Related analyzers (different jobs)

| Id / analyzer | Job |
|---------------|-----|
| SourceGenerators **TW0001** `FileNameRuleAnalyzer` | Generic `.cs` kebab basename (`TW*` package family) |
| Architecture **TWA0001** | Partial class primary/secondary **declaration shape** |
| Architecture **TWA0015/0016** | Axis-1 feature/platform grammar |

## Results

- **Policy:** Razor Pascal exception + TW0001 / Ganda-188 enforcement split documented in
  `AGENTS.md` and `documentation/developer/standards/file-naming.md`.
- **Remediations:** ~79 path renames across web-server, web-spa, api-server integration tests and
  aspire-tests; `badge-status.cs` renamed.
- **Build:** `dev build` **0 warnings / 0 errors**.
- **Not done here (accepted deferrals):** enable `TW0001` (upstream multi-dot — source-generators
  task **021**); optional doc HowTo/logo renames.
- **Already done elsewhere (not this repo):** Ganda task **188** `kebab-path-names` audit check
  (completed before/around this wave; available via `ganda repo audit`).
- **Review:** implementer self-check on renames + build; no multi-round peer review kitchen
  (mechanical renames + docs).

## Session

- Created: Grok analysis + task open (2026-07-29)
- Updated: TW0001 SSOT corrections; Ganda TW0001 doc fix
- Implementation: Grok orchestrate 133 (2026-07-29) — renames + policy + build
