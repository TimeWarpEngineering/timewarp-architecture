# Findings — 126 post-dogfood review

Evidence gathered 2026-07-25 by two parallel read-only survey agents (folders/grammar surface and
packaging surface), per [plan.md](plan.md) Phase 1. Every claim cites a repo path, count, commit
hash, or quoted task-history line; "recorded in task history" vs "observed in current repo state"
is distinguished throughout.

## 1. Feature-cohesive folders + filename grammar

### Wins

- **Zero grammar-caused renames in the tree's entire history.** `git log --oneline -- source/container-apps/web/features` returns exactly 6 commits total: `8eae0006` (ship registry+guard+TWA0015/16), `defa664f` (bulk rehome — the migration itself), `b70b0616` (Profile mapping), `f6d80f3f` (identity EF persistence, +124 lines/2 new infra files), `7e196656`, `a7cb2977` (GoldenDbContext→AggregateDbContext rename, touched 2 feature files for 2-3 line base-class references only). `git log -M --diff-filter=R --name-status` over that path returns **zero renames**. Every file landed with a correct grammar name on first commit — no observed "learned the naming by trial and error" pattern.
- **Generated MSBuild props match the JSON registry exactly, no staleness observed.** `source/container-apps/web/msbuild/feature-filename-grammar.g.props` lists the same 5 layers and 3 functions (`handler→application`, `endpoint→server`, `feature-annotations→server`) as `feature-filename-grammar.json`, and `feature-membership.targets` (read in full) only imports the generated props and runs a single regex-based guard gated on `web-server` — it does **not** hand-duplicate globs. This matches the round-1 review's M1 fix disposition (see Frictions below) — the SSOT gap that review found has been closed in current repo state.
- **SPA exception holds.** `find source/container-apps/web/web-spa/features -maxdepth 1 -type d` lists 20 conventional slice folders (account, admin, chat, counter, identity, profiles, …); spot-check of `web-spa/features/identity/pages/passkeys-page/` shows `PasskeysPage.razor` + `PasskeysPage.razor.cs` — ordinary PascalCase Blazor naming, no `-layer` suffix anywhere. No grammar leakage into the SPA tree observed.
- **Cross-vendor review process actually ran and closed real issues.** `kanban/done/114-002-.../review/round-1/general.md` (Claude reviewing a Grok implementation, per its own header) raised 3 issues; `merged.md` and `disposition.md` show all 3 fixed on the same task id, and a fully independent `round-2/orchestrator-verification.md` empirically re-ran `dev build` (0/0), the full `dev test` (548 passed/0 failed) and template-smoke rather than trusting the summary.

### Frictions

- **F-cost, recorded historically:** the 114-001 spike (`kanban/done/114-001-...md`, Results section) states the analyzer path-normalization pitfall ("SyntaxTree.FilePath for glob-included files arrives project-relative WITH `..` traversal... the spike's exclusion heuristic silently ate the whole cohesive tree") **"cost an hour"** — the spike's own words.
- **Round-1 review of 114-002 found a real SSOT violation at the time:** `general.md` Issue 1 — "the targets still hand-list every `Compile` glob... Generated function items are unused... Adding a layer to the JSON would update the error-message layer list while globs/match stay stale." This was real (the issue text quotes exact line ranges `feature-membership.targets:18-38,62-72,81-89`) and was fixed same-task per `merged.md` M1 disposition note. Current repo state (read directly) confirms the fix held — no re-drift found today.
- **Round-1 also found stale doc examples:** Issue 3 — `skills/tw-web-api-contracts/SKILL.md:218,235` still showed pre-grammar filenames (`queries/get-*.cs` without `-contracts` suffix) after the migration landed. Fixed per M3 disposition note.
- **Layer/subfolder inconsistency, observed directly (not recorded in any task):** contracts get `commands/`/`queries/` subfolders, but application/infrastructure/server files for the same operations sit flat in the slice root. Example — `admin/roles/`: contracts split into `commands/create-role-contracts.cs`, `commands/delete-role-contracts.cs`, `commands/update-role-contracts.cs`, `queries/get-role-contracts.cs`, `queries/get-roles-contracts.cs` (5 files, subfoldered), while `create-role-handler-application.cs`, `delete-role-handler-application.cs`, `get-role-handler-application.cs`, `get-roles-handler-application.cs`, `role-details-contracts.cs`, `role-store-application.cs`, `roles-feature-annotations-server.cs`, `update-role-handler-application.cs` (8 files) sit flat at `admin/roles/` root. Same pattern in `identity/`: 11 command contracts + 3 query contracts live in subfolders, but all 19 application/infrastructure/server files sit flat at `identity/` root — a newcomer looking beside `identity/commands/add-agent-key-contracts.cs` for its handler will not find it there; it's one level up as `identity/add-agent-key-handler-application.cs`. This asymmetry is not called out anywhere in `tw-feature-placement/SKILL.md`'s worked examples (which show single files, never a whole-slice tree).

### Risks

- **Domain layer is registered, globbed, and has build machinery pointed at it, but has zero product files under `web/features/` today** (`find ... -name "*-domain.cs"` → 0). Either intentionally reserved for aggregate roots that haven't landed yet, or dead configuration surface — repo state alone can't distinguish the two.
- **`endpoint` is a registered function (→ `server`) with zero files using it.** The skill documents it as reserved ("the template currently generates FastEndpoints from contracts rather than hand-authoring them, so use this only for a genuinely hand-written endpoint" — `skills/tw-feature-placement/SKILL.md:60`), so this looks like intentional headroom rather than drift, but it is unused registry surface today.
- **No analyzer governs the `commands/`/`queries/`/flat subfolder choice** — TWA0015/TWA0016 and the membership guard key only off filename suffixes (confirmed: `feature-filename-grammar.json` has no folder-path concept, and `feature-membership.targets` matches on `%(Filename)` only). The commands/queries subfoldering is therefore convention-by-memory layered on top of a machine-enforced grammar — exactly the pattern AGENTS.md's standing directive (prefer analyzers/generators over agreement-by-memory) flags for enforcement, yet it isn't enforced here.
- **New-file misplacement is caught at build, not creation** — documented, accepted risk (ADR-0008 Negative Consequences; spike finding 4 confirms it was observed live during IDE testing). Not a surprise, but a standing rough edge for agents authoring files interactively.

### Open questions

- Should the `commands/`/`queries/` subfolder convention become a fourth grammar axis (folder, not just filename) with its own analyzer, given it's currently unenforced tribal structure inside an otherwise fully machine-enforced scheme?
- Is the empty `domain` layer intentional reserved headroom or should it be dropped from the registry until a real aggregate-root product file needs it?
- Is `endpoint` (0 uses) worth keeping registered now, or should it wait until the first hand-authored endpoint actually appears?

### Candidate decisions

1. F1 — keep: bulk-migrate-then-glob approach (ADR-0008) produced zero post-migration renames across 6 commits of real feature work (identity EF persistence, Profile mapping) — evidence: `git log -M --diff-filter=R` empty result, commit hashes above.
2. F2 — keep: SSOT registry → generated props/analyzer-constants pipeline, once the round-1-found hand-duplication (M1) was fixed, shows no drift today — evidence: `feature-filename-grammar.g.props` vs `feature-filename-grammar.json` byte-for-byte layer/function match; `feature-membership.targets` contains no hand-listed globs.
3. F3 — tweak-candidate: contracts-get-subfolders-but-other-layers-stay-flat asymmetry is undocumented in the skill's worked examples and unenforced by any analyzer — evidence: `admin/roles/` and `identity/` directory listings above.
4. F4 — restructure-candidate or drop-candidate: `domain` layer has zero product files ever (evidence: `find ... -name "*-domain.cs"` → 0 hits) despite being a registered layer with its own csproj glob and membership-guard entry.
5. F5 — keep-as-is-candidate: `endpoint` function (0 uses) is explicitly documented as intentional reserved headroom in the skill itself, not repo drift — evidence: `skills/tw-feature-placement/SKILL.md:60`.
6. F6 — tweak-candidate: the analyzer path-normalization pitfall that "cost an hour" during the spike and the MSBuild incremental-staleness gotcha are both now permanently absorbed as standing documentation/warnings rather than one-time surprises — evidence: `kanban/done/114-001-....md` Results items 1–2, cross-referenced into `kanban/done/114-002-.../review/round-2/orchestrator-verification.md` ("BINDING spike requirement — path-pitfall tests: PRESENT").

**File-count evidence table (mechanical, from repo state 2026-07-25):**

| Layer suffix | File count | Breakdown |
|---|---|---|
| `-contracts.cs` | 37 | includes `role-details-contracts.cs` and similar shared-shape files at slice root by design (skill explicitly names this pattern) |
| `-application.cs` | 27 | 24 `*-handler-application.cs` (registered archetype) + 3 escape-hatch (`role-store-application.cs`, `web-authn-payload-decoder-application.cs`, `web-authn-relying-party-selection-application.cs`) |
| `-server.cs` | 6 | all 6 are `*-feature-annotations-server.cs`; 0 use the registered-but-reserved `endpoint` function |
| `-infrastructure.cs` | 3 | all 3 are escape-hatch (`credential-entity-type-configuration-infrastructure.cs`, `principal-entity-type-configuration-infrastructure.cs`, `profile-entity-type-configuration-infrastructure.cs`) — infrastructure has no registered function in the registry at all |
| `-domain.cs` | 0 | none — see Risks |
| Total | 73 | escape-hatch : registered-archetype-with-function ratio = 6 : 30 (≈16.7% of the 36 non-contract files are escape-hatch) |

Slice sizes (file count including subfolders): identity 33 (largest), admin/roles 13, profile 4. `v2/` contains only `overview.md`, no code — confirmed in 114-002 round-1 review as "legit pre-existing versioning doc that moved with the tree," not a stray slice.

## 2. Platform packaging dual-mode

### Wins

- Composed sourceName-safe package IDs work as designed: `msbuild/timewarp-platform-packages.props` builds `TwArchitectureAnalyzersPackageId`/`GeneratorsPackageId`/`AttributesPackageId` from `_TwPlatformVendor` + `.Architecture.*` so no continuous `TimeWarp.Architecture` literal exists in PackageReference/PackageVersion; independently re-verified by a second reviewer in `kanban/done/115-.../review/round-2/orchestrator-verification.md:7-14` ("BOTH halves of the bug are fixed").
- `dev template-smoke` (`tools/dev-cli/endpoints/template-smoke-command.cs`) is a real, CI-wired regression gate: packs template + all platform packages at a unique `2.0.0-smoke` version into a local feed, generates two apps named `SmokeDefault`/`SmokeNoPostgres` (deliberately ≠ sourceName so rewrite bugs surface), asserts no `<AppName>.Analyzers/.Generators/.Attributes` fragments exist anywhere, then restores/builds 0/0. `.github/workflows/template-smoke.yml:6-27` runs it on push/PR to master (path-filtered) and `workflow_dispatch`.
- Task 124 produced a real end-to-end proof outside the monorepo: "published beta.7 template nupkg from flatcontainer → dotnet new in a clean hive → generated app pins beta.7 → restored against nuget.org ONLY → Build succeeded, 0 Warnings, 0 Errors" (`kanban/done/124-...md:60-63`).
- Identity's OIDC trusted-publishing concern (repo+workflow scoped, not per-PackageId) was validated in practice: "rode the repo-scoped OIDC trusted publishing with zero credential work, exactly as Steve said it would" (`kanban/done/124-...md:49-50`).
- The Generators-package attach surface is not static/stale despite the docs implying otherwise — each additional attach point carries an inline task citation (task 106 TypedId on `web-domain`, task 107 `IngressRoutePrefixGenerator` on `yarp.csproj:57` and `aspire-app-host.csproj:84`, task 104-027 on `timewarp-identity.csproj:12`), i.e., the split is being deliberately extended, not accreted by copy-paste.

### Frictions

- **Confirmed stale comment (the tipped lead), plus two more like it.** `Directory.Packages.props:36-44` still reads: *"TimeWarp.Identity has never been published (task 104-003 is its first consumer beyond the library itself) — there is no 'last published version' to pin to yet... Bump/verify against the actually-published version at first release."* Task 124 shipped Identity's first publish at beta.6 (`kanban/done/124-...md:49`). Two sibling comments describe the same superseded policy: `Directory.Packages.props:20-23` ("Pinned to the last PUBLISHED foundation version — which may lag the repo's in-dev `<Version>`... Bump these when a new foundation version is published") and `:30-32` ("Pinned to last PUBLISHED version — may lag the repo's in-dev `<Version>`"). All three describe the pre-124 "lag behind published" policy, even though the actual pin values today (`Foundation.*`, `Modules`, `Analyzers`, `Generators`, `Attributes`, `Identity` all at `2.0.0-beta.7`) correctly match the current `<Version>` (`source/Directory.Build.props:14`, `timewarp-templates/Directory.Build.props:7`) per the new task-124 policy documented in AGENTS.md. The mechanics are right; the comments sitting next to them are wrong and would mis-teach the next person to touch that file.
- **`HowToUpgradeToAnalyzerPackages.md` understates the real attach surface.** It says: *"Generators must not be referenced repo-wide — only on projects that should run them (`web-spa`, `api-server`)"* (line 83-84) and its consumer table (lines 50-54) lists only `web-spa`/`api-server`/`api-contracts`. Actual grep shows 8 non-generator-project consumers of `TwArchitectureGeneratorsPackageId`: `web-domain`, `web-infrastructure`, `web-spa`, `web-server`, `api-server`, `aspire-app-host`, `yarp`, and `timewarp-identity`. The doc is a stale snapshot of an earlier attach list.
- **The release took two version bumps, same day, due to a sequencing bug.** `kanban/done/124-...md:45-46`: *"Took TWO releases — the beta.6 template packed before the pin bump (chicken-and-egg the task sequencing missed)."* Commits `7cad5fcb` (11:49) and `6f26e042` (13:32) are ~1h43m apart on 2026-07-24; the second commit message states plainly: "beta.6 template packed BEFORE the pin bump, so its generated apps still pin beta.2 - the exact staleness class this policy kills."
- **`ganda runfile cache` staleness bit the smoke gate reviewer.** `review/round-2/orchestrator-verification.md:26-28`: `dev template-smoke` failed as "Unknown command" on a machine with a stale ganda runfile cache, needing `ganda runfile cache --clear`; noted as cosmetic since CI invokes `dotnet run` directly, not `ganda`.
- **`template-smoke` structurally cannot catch the actual failure mode that broke real users.** Its own Design region says so: *"Published nuget.org pins lag monorepo API surface (e.g. `Entity<TId>`, `EndpointAllowAnonymous`); smoke validates 'this branch's template + this branch's packages'"* (`tools/dev-cli/endpoints/template-smoke-command.cs:11-15`), by packing local `2.0.0-smoke` copies of every platform package rather than exercising the real pinned versions against nuget.org. Task 124's description states this directly: *"`dev template-smoke` cannot see this by design (it packs the monorepo into a local feed)"* — real greenfield apps were broken (CS0234, `TimeWarp.Identity` 404) while `dev template-smoke` stayed green throughout. The only thing that ever catches this class of break is the manual "generate outside the monorepo against real nuget.org" step task 124 performed once by hand — it is not a repeatable CI gate.
- **The sourceName-rewrite fix pattern has already needed a repeat.** Task 115 established the "compose the platform namespace via MSBuild property, never write a continuous `TimeWarp.Architecture` literal" pattern. Commit `a251980f` ("fix(template): 113 review round-2 - sourceName-safe TypedIds.Ef using") had to apply the identical pattern again for `TwArchitectureTypedIdsEfNamespace` (used at `web-infrastructure.csproj:33`) — every new platform-namespace literal introduced into template content is a fresh opportunity to reintroduce the same class of bug, and there's no analyzer/build check that forces new literals through the composed-property path (it was caught by a human reviewer in round 2, not automatically).

### Risks

- Comment/policy drift (the three stale comments above) is low-severity today because the values happen to already be correct, but the pattern — narrative comments that don't get updated when the underlying policy changes — is exactly what caused the original beta.5/beta.6 CS0234 break per task 124's description (pins "several lag at beta.2" despite AGENTS.md's stated intent).
- Nothing currently prevents a new template-content literal (a new namespace, a new package reference) from reintroducing the sourceName-rewrite class of bug except human review; task 115's fix and its task-113 repeat were both manual catches, not compiler-enforced.
- `dev template-smoke` gives a false sense of "packaging is verified" coverage in CI while structurally exempting itself from the exact real-world failure (stale nuget.org pins) that shipped to real users between beta.5 and beta.7.

### Open questions

- Is there a plan to make the "generate outside the monorepo against real nuget.org" verification (performed manually once in task 124) into any kind of repeatable/scheduled check, or is it accepted as a release-time manual step permanently?
- Should there be a build-time or analyzer check that flags any new literal use of `TimeWarp.Architecture.*` / `TimeWarp` platform namespaces in template-shipped `.cs`/`.csproj` content that isn't routed through `timewarp-platform-packages.props`, given the pattern has already repeated once (task 113 round 2)?
- Is `timewarp-templates/Directory.Packages.props` (which disables CPM and pins its own older inline versions for "the template-test harness") a second packaging surface that could itself drift from the root pins, given it's explicitly *not* using the composed properties or the pins==release-version policy?

### Candidate decisions

1. P1 (tweak): Refresh the three stale "lag behind published" comments in `Directory.Packages.props:20-23,30-32,36-44` to reflect the task-124 pins==release-version policy — evidence: quoted lines above vs. `kanban/done/124-...md:51-52` and `AGENTS.md` "CPM `PackageVersion` pins... equal the release `<Version>`".
2. P2 (tweak): Update `documentation/developer/how-to-guides/HowToUpgradeToAnalyzerPackages.md:50-54,83-84` consumer table/claim to match the actual 8-project Generators attach surface — evidence: grep of `TwArchitectureGeneratorsPackageId` across `source/`.
3. P3 (restructure-candidate): Consider whether the "generate outside the monorepo against real nuget.org" check from task 124 should become an automated (even if low-frequency/scheduled) gate, since `dev template-smoke` is explicitly designed not to catch that failure class — evidence: `template-smoke-command.cs:11-15` Design region + task 124 description's "cannot see this by design."
4. P4 (keep): The composed-property sourceName-safety pattern itself is sound and has been independently re-verified twice (task 115 round 2, task 113 round 2 repeat) — evidence: `msbuild/timewarp-platform-packages.props`, `review/round-2/orchestrator-verification.md:7-14`, commit `a251980f`.

## 3. Consolidated candidate decisions (feed into rfc/rfc.md)

| # | Tag | One-liner |
|---|-----|-----------|
| F1 | keep | Bulk-migrate-then-glob (ADR-0008): zero post-migration renames across all feature work |
| F2 | keep | Registry SSOT → generated props pipeline: no drift after M1 fix |
| F3 | tweak | commands/queries subfolder asymmetry: undocumented + unenforced; document and/or enforce |
| F4 | restructure/drop | `domain` layer registered but zero product files ever — reserved headroom or dead surface? |
| F5 | keep | `endpoint` function reserved-unused is documented intent, not drift |
| F6 | keep | Spike-era pitfalls (path normalization, incremental staleness) institutionalized as docs/tests |
| P1 | tweak | Three stale "lag behind published" comments in Directory.Packages.props contradict task-124 pin policy |
| P2 | tweak | HowToUpgradeToAnalyzerPackages.md consumer table stale (3 listed vs 8 actual Generators consumers) |
| P3 | restructure | Automate the "generate against real nuget.org" release proof that template-smoke structurally can't cover |
| P4 | keep | Composed-property sourceName-safety pattern sound (verified twice) |

RFC author may additionally promote packaging open questions 2 (analyzer/build check for new
platform-namespace literals in template content — the bug class has already repeated once) and 3
(`timewarp-templates/Directory.Packages.props` as a second, unpinned packaging surface) into
balloted decisions if they judge the evidence sufficient.
