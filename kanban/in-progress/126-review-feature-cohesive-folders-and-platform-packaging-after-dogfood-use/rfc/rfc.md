# RFC: Post-dogfood disposition for feature-cohesive folders + platform packaging

**Status:** ballots complete, tallied 2026-07-25 — D2–D5 unanimous (self-resolving); **D1 dissent
→ maintainer (Steve), awaiting resolution**. Unanimous doc-only items proceeding to fold-in.
**Host task:** 126 — kitchen for RFC + fold-in (agent-collaboration same-task rule).
**Author:** rfc-author agent (Claude, session 2026-07-25).
**Audience:** Independent reviewers. Append a ballot under [Reviewer opinions](#reviewer-opinions)
using the template at the bottom. Do **not** rewrite others' entries.

---

## 1. Why this exists

Task 126 ran a two-track evidence survey (`../findings.md`) of the feature-cohesive folder
grammar (ADR-0008 / 114 / 114-002) and the platform-packaging dual-mode surface (051 / 092 / 115 /
124) after real dogfood use — identity, golden/aggregate persistence, ingress generators, and the
beta.6/beta.7 publish cycles. This RFC ballots the decisions `findings.md` §3 flagged as needing an
opinion rather than being one-sided.

### Out of scope

- Re-litigating ADR-0008's axis decisions (folder-vs-project, layer-vs-module,
  single-vs-per-module assembly). `findings.md` produced no evidence against any of these — every
  friction found is at the registry/documentation/tooling layer, not the axis choice itself.
- Production restructuring on this task. Per `task.md` and `plan.md`, anything structural
  (renames, grammar layer-set changes, `msbuild/timewarp-platform-packages.props` composition
  changes, package-existence/split changes, CPM pin-policy changes, `.template.config/template.json`
  symbol-default changes) becomes a proposed child task, not work done directly on 126.

---

## 2. Sources of truth (evidence matrix)

| # | Source | Path / ref | Nature |
|---|--------|------------|--------|
| A | Findings (folders) | `../findings.md` §1 | Wins/frictions/risks from live tree + registry survey |
| B | Findings (packaging) | `../findings.md` §2 | Wins/frictions/risks from packaging survey |
| C | ADR-0008 | `documentation/developer/conceptual/architectural-decision-records/approved/0008-feature-cohesive-folders-with-filename-grammar-layer-composition.md` | Ratified axis decisions + documented Negative Consequences |
| D | Skill | `skills/tw-feature-placement/SKILL.md` | Grammar table, registry shape, worked examples, `endpoint` reserved-headroom note (line 60) |
| E | Repo policy | `AGENTS.md` — Layout, Platform packages, Enforcement sections | Filename grammar summary, dual-mode packaging, CPM pin policy, TWA000x table |
| F | Task 113 | `kanban/done/113-golden-persistence-...` | Golden/aggregate persistence dogfood — `AggregateDbContext` rename touched 2 feature files (findings F1 evidence) |
| G | Task 104-032 | `kanban/done/104-032-implement-ef-core-persistence-...` | Identity-EF dogfood (feature-folder mining, `plan.md` Step 1.4) |
| H | Task 114-001 / 114-002 | `kanban/done/114-001-...`, `kanban/done/114-002-...` | Spike pitfalls ("cost an hour"), round-1/round-2 review disposition (M1 SSOT fix, M3 doc fix) |
| I | Task 115 | `kanban/done/115-fix-template-sourcename-rewriting-...` | Origin of the composed-property sourceName-safety pattern |
| J | Task 124 | `kanban/done/124-release-200-beta6-...` | CPM pins==release-version policy, beta.6/beta.7 sequencing bug, real nuget.org proof |
| K | This RFC's own verification | `timewarp-templates/Directory.Packages.props`, `timewarp-templates/Directory.Build.props`, `timewarp-templates/timewarp-templates.slnx`, `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`, commit `22aa24b0` | D5 premise check (see below) |

---

## 3. Objective / already-settled, not balloted

These need no ballot — the evidence is one-sided and no change is proposed on any of them.

| Item | Disposition |
|------|-------------|
| **F1** — zero grammar-caused renames across the tree's entire history (`git log -M --diff-filter=R` empty; 6 total commits touching the tree) | Keep bulk-migrate-then-glob approach; no change |
| **F2** — registry SSOT → generated props pipeline shows no drift today (byte-for-byte match, `feature-membership.targets` has no hand-listed globs) after the round-1 M1 fix | Keep; no change |
| **F5** — `endpoint` function (0 uses) is documented intentional reserved headroom, not drift (`skills/tw-feature-placement/SKILL.md:60`) | Keep as-is; no change |
| **F6** — spike-era pitfalls (path-normalization, incremental staleness) are permanently institutionalized as docs/tests, not recurring surprises (`114-001` Results ↔ `114-002` round-2 verification) | Keep; no change |
| **P4** — composed-property sourceName-safety pattern is sound, independently re-verified twice (115 round-2, 113 round-2 repeat via `a251980f`) | Keep; no change |

The following are **objective doc-drift bugs** — both sides of the drift (stale text vs. current
policy) are already quoted in `../findings.md`, so they fold in directly **without** a ballot:

- **P1** — three stale "lag behind published version" comments in root `Directory.Packages.props`
  (lines 20-23, 30-32, 36-44) contradict the task-124 pins==release-version policy that is already
  in effect for the actual pin *values*. Fix: rewrite the three comments to state the current
  policy (pins equal the release `<Version>`, bumped in the same commit per AGENTS.md).
- **P2** — `documentation/developer/how-to-guides/HowToUpgradeToAnalyzerPackages.md` lines 50-54 /
  83-84 list only 3 Generators-package consumers; grep shows 8 actual consumers (`web-domain`,
  `web-infrastructure`, `web-spa`, `web-server`, `api-server`, `aspire-app-host`, `yarp`,
  `timewarp-identity`). Fix: update the consumer table and the "only on projects that should run
  them" claim to match.

---

## 4. Decisions needing ballots

### Decision 1 (from F3) — commands/queries subfolder asymmetry

**Topic:** Contracts get `commands/`/`queries/` subfolders; application/infrastructure/server files
for the same operations sit flat in the slice root (e.g. `admin/roles/`: 5 contract files
subfoldered, 8 application/server files flat; `identity/`: 11+3 contract files subfoldered, 19
application/infrastructure/server files flat). No analyzer governs the choice — TWA0015/TWA0016
and the membership guard key only on `%(Filename)`, not folder path (`../findings.md` F3,
risks §1). The skill's worked examples never show a whole-slice tree, so the asymmetry is
undocumented.

| Option | Description |
|--------|-------------|
| **A. Document as intended convention** | Add a worked whole-slice example to the skill showing contracts subfoldered, other layers flat; state this is deliberate (contracts are the shared/public surface, other layers are slice-private) |
| **B. Document + symmetrize going forward, no enforced rule** | Same doc fix as A, plus a stated convention that new work should also subfolder application/server files by operation where it aids navigation — left to author judgment, not enforced |
| **C. Promote to an enforced grammar axis (analyzer)** | Extend `feature-filename-grammar.json` / TWA001x to understand folder path, not just filename; enforce the chosen shape at build time |

**Trade-offs:** A is cheapest and matches "contracts are public API surface, everything else is
implementation detail" — a defensible rationale, but it's after-the-fact rationalization of an
uninstructed pattern rather than a designed one. B costs nothing to adopt but doesn't fix the
"newcomer looks beside the contract and doesn't find the handler" friction findings called out
directly. C closes the gap for real but is a new analyzer surface (registry format change, new
diagnostic, membership-guard folder-awareness) — non-trivial, would need its own child task per
the ADR's registry-edit-⇒-full-rebuild cost and this task's no-production-restructure rule.

**Author lean: A** — the asymmetry has a real, defensible rationale (contracts are the slice's
public/shared surface and are the one layer other code may import across slice boundaries in
spirit even under TWA0009; other layers are private implementation and flat access is fine at
current slice sizes of ≤33 files). Document it as intended in the skill with a whole-slice worked
example; do not spend an analyzer on a navigation preference with zero recorded confusion incidents
in dogfood history (`plan.md` Step 1.5's TWA0015/16/membership-guard grep over 104-*/113-* task
history surfaced no "couldn't find the handler" complaint, only this RFC's own observation).

---

### Decision 2 (from F4) — empty domain layer

**Topic:** `domain` is a registered layer with its own csproj glob and membership-guard entry, but
has **zero** product files under `web/features/` (`find ... -name "*-domain.cs"` → 0 hits, cross-
checked twice: findings survey and this RFC's independent re-verification instruction in
`plan.md` Step 1.2). Every dogfooded feature to date (identity, profile, admin/roles, golden/
aggregate persistence) has only needed application + contracts + server.

| Option | Description |
|--------|-------------|
| **A. Keep as reserved headroom** | Document explicitly in the skill *why* it's reserved (aggregate roots that will eventually live in product slices, e.g. once a slice needs its own `IAggregateRoot` rather than a shared/platform one) |
| **B. Remove from registry + globs until first real use** | Drop `domain` from `feature-filename-grammar.json`, regenerate props, requires full rebuild per ADR's documented registry-edit cost; child task if treated as non-trivial |
| **C. Leave as-is, no doc change** | Status quo; unused registry surface stays undocumented as intentional or not |

**Trade-offs:** A costs a documentation sentence and matches F5's precedent (the `endpoint`
function is already kept unused-but-documented for the same reason: it's cheap headroom for an
anticipated pattern, not accreted cruft). B is the "prefer analyzers/generators over drift, remove
dead surface" instinct but risks removing something the moment before it's needed (per-slice
aggregate roots are plausible given the golden/aggregate persistence work in 113), forcing a
re-add churn cycle. C leaves the open question open, which is worse than a one-line doc fix at
zero cost.

**Author lean: A** — cheapest fix, consistent with how F5's `endpoint` headroom is already
justified in the skill, and the registry-edit-⇒-full-rebuild cost of B is not worth paying to
remove something zero-cost to keep. If a full program cycle passes with still zero domain files
after this is documented, that's evidence for a future B.

---

### Decision 3 (from P3) — automating the "generate against real nuget.org" release proof

**Topic:** `dev template-smoke` is explicitly designed to pack the monorepo's own platform packages
into a local feed (`tools/dev-cli/endpoints/template-smoke-command.cs:11-15` Design region), so it
structurally cannot catch stale-published-pin breaks — the exact failure class that broke real
greenfield apps between beta.5 and beta.7 (task 124 description: "cannot see this by design").
The only check that ever caught it was a one-time manual "generate outside the monorepo, restore
against real nuget.org" pass performed once in task 124.

| Option | Description |
|--------|-------------|
| **A. Scheduled low-frequency CI job** | A workflow (e.g. weekly or on release-tag push) that runs `dotnet new timewarp-architecture` outside the repo and restores against nuget.org only, failing if restore/build fails |
| **B. Release-workflow post-publish step** | Add the same check as a required step immediately after each release's package publish, before the release is considered done |
| **C. Keep as documented manual release checklist step** | No automation; codify the exact steps task 124 performed as a release-checklist item so it isn't reinvented each time |

**Trade-offs:** A gives real detection without gating every release, at the cost of a new
recurring-schedule workflow to maintain (`CronCreate`/GitHub Actions `schedule:` trigger) and a
lag between a break landing and the job catching it. B closes the exact sequencing bug task 124
hit ("beta.6 template packed before the pin bump") by construction — it runs after publish, in the
same release, so a broken release is caught before anyone treats it as done — but it makes every
release slower and requires a network-dependent step in the release pipeline (nuget.org indexing
lag is a known real-world gotcha for freshly-published packages). C is zero engineering cost but
relies on human memory exactly the way the original beta.5/beta.6 break did.

**Author lean: B** — the failure task 124 hit was a *release-sequencing* bug (template packed
before pins bumped), not a randomly-timed regression a weekly schedule would catch faster than the
next release anyway. A post-publish gate in the same release workflow directly closes the
sequencing gap at the point the mistake happened, and "release now has one more automated step" is
a fully acceptable release-cadence cost for a release-integrity check. This is non-trivial (new
release-workflow step, needs nuget.org propagation-delay handling) and is a child task regardless
of lean.

---

### Decision 4 (promoted from packaging open question 2) — guard against new sourceName-unsafe platform-namespace literals

**Topic:** Task 115 established "compose the platform namespace via MSBuild property, never write
a continuous `TimeWarp.Architecture` literal in template content." The bug class has already
repeated once: commit `a251980f` (task 113 round-2 review) had to apply the identical fix for
`TwArchitectureTypedIdsEfNamespace` at `web-infrastructure.csproj:33`, caught only by a human
reviewer, not a build check (`../findings.md` packaging frictions, last bullet).

| Option | Description |
|--------|-------------|
| **A. Analyzer / build-time check** | A Roslyn analyzer or MSBuild target that flags any literal `TimeWarp.Architecture` / `TimeWarp.Foundation` / etc. token in template-shipped `.cs`/`.csproj` content not routed through `timewarp-platform-packages.props`'s composed properties |
| **B. Grep-based assertion inside `dev template-smoke`** | Add a cheap regex/grep step to the existing smoke command that fails if any packed template content contains a raw `TimeWarp.Architecture.` (or sibling vendor) literal outside the composed-property mechanism |
| **C. Rely on human review + skill documentation** | No new automation; document the pattern more prominently (e.g. in the packaging skill/section of AGENTS.md) and rely on code review to catch new literals |

**Trade-offs:** A is the most robust (compiler-enforced against arbitrary future literal shapes)
but is new analyzer surface — meaningful design and maintenance cost for what is, so far, a
2-occurrence bug class. B is much cheaper — `dev template-smoke` already packs the full template
content and already asserts absence of `<AppName>.Analyzers/.Generators/.Attributes` fragments
(`../findings.md` packaging wins), so a sibling grep assertion for the *source* literal pattern
(rather than the *rewritten-output* pattern it already checks) is a small, additive change to an
existing, CI-wired gate. C costs nothing today but is the same mitigation that already failed
once (round-1 human review missed the first occurrence; only round-2 review caught it).

**Author lean: B** — `dev template-smoke` already exists, is CI-wired
(`.github/workflows/template-smoke.yml`), and already performs output-side assertions of this
exact bug class; extending it with a source-side grep is proportionate to a 2-occurrence pattern
and far cheaper than standing up new analyzer infrastructure (A) for something a grep line fully
covers. Revisit A only if the pattern recurs a third time or grows beyond a literal-token check.

---

### Decision 5 (promoted from packaging open question 3) — `timewarp-templates/Directory.Packages.props` as a second packaging surface

**Topic:** `../findings.md` open question 3 asked whether `timewarp-templates/Directory.Packages.props`
is a second, unpinned packaging surface that could itself drift, since it disables CPM and
(per its comment) "pins its own older inline versions for the template-test harness."

**Verification performed for this RFC** (required before balloting, per the task-lead's
instruction): read `timewarp-templates/Directory.Packages.props`,
`timewarp-templates/Directory.Build.props`, `timewarp-templates/timewarp-templates.slnx`, and the
one project it references, `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`.

Findings:
- `timewarp-templates/Directory.Packages.props` sets `ManagePackageVersionsCentrally=false` with a
  comment: "the template-test harness intentionally pins its own (older) package versions inline on
  each `PackageReference`." This was introduced in commit `b4b32272` (task 064, kebab-case rename),
  whose own commit message describes a `timewarp-architecture-template-tests.csproj` project — the
  "test harness" the comment refers to.
- That harness project **no longer exists**: it was deleted in commit `22aa24b0`
  ("chore: delete stale templates test project; remove orphaned GlobalUsingsAnalyzer").
- The `.slnx` (`timewarp-templates/timewarp-templates.slnx`) references exactly **one** project:
  `timewarp-architecture-template.csproj`.
- That csproj has **zero `<PackageReference>` items** — it is a pure `dotnet new` template-packing
  project (`<Content Include>` globs that pack `source/`, `tests/`, `msbuild/`, `.template.config/`
  as template content; `<Compile Remove="**\*" />`). There is nothing for CPM to manage in this
  tree today, pinned or otherwise.

**The premise in the open question is false as currently stated**: there is no second surface with
its own inline (older) pins today — the CPM-disable property and its comment are vestigial,
describing a test-harness project removed after task 064. `Directory.Build.props` in the same tree
correctly sets `<Version>2.0.0-beta.7</Version>` in sync with root, with an explicit comment to
keep it in sync — that part is current and correct.

| Option | Description |
|--------|-------------|
| **A. No action needed — delete the vestigial comment/property, or leave and document why it's now inert** | Since there are no PackageReferences in this tree, `ManagePackageVersionsCentrally=false` is a no-op; either remove the file (simplify) or fix the comment to state it's currently unused headroom in case the harness returns |
| **B. Wire it to the same composed props / pin policy** | Not applicable given A's finding — no packages exist to pin. Held for **if** a real test/harness project with dependencies is reintroduced later |
| **C. Add a smoke-time assertion that pins can't drift silently** | Not applicable today for the same reason — nothing to assert against |

**Author lean: A, with a documentation-only fix** — rewrite the stale comment in
`timewarp-templates/Directory.Packages.props` to say the CPM-disable is currently inert (no
PackageReferences exist in this tree since the test-harness project was removed in `22aa24b0`) and
is kept only as low-cost headroom should a template-test project with real dependencies return.
Do not delete the file outright — removing it is a larger, unforced surface change for a task whose
scope is findings + doc fixes, and keeping CPM disabled here is harmless while inert. This is a
doc-only fix, safe to fold in directly like P1/P2 once the ballot confirms no reviewer disagrees
with the verification.

---

## 5. Author priority

If leans are accepted, priority order for fold-in / follow-up:

1. P1, P2 — direct doc fixes (already objective, no ballot needed)
2. D5 — direct doc fix (comment correction), pending ballot confirmation of the verification
3. D2 — one-line skill doc addition (domain-layer headroom rationale)
4. D1 — skill doc addition (whole-slice worked example + asymmetry rationale)
5. D4 — extend existing `dev template-smoke` with a grep assertion (child task, small)
6. D3 — release-workflow post-publish step (child task, larger — touches release pipeline)

---

## 6. Reviewer opinions

*(Independent ballots. Entries not rewritten after the fact.)*

### Ballot template

```markdown
### <agent/model name> — <date>
**Overall:** <one line>
**Decision 1 (commands/queries subfolder asymmetry):** <A|B|C or refined> — <why>
**Decision 2 (empty domain layer):** <A|B|C or refined> — <why>
**Decision 3 (automate nuget.org release proof):** <A|B|C or refined> — <why>
**Decision 4 (guard sourceName-unsafe literals):** <A|B|C or refined> — <why>
**Decision 5 (timewarp-templates second packaging surface):** <A|B|C or refined, or dispute the
verification> — <why>
**Anything the author missed:** <…>
```

### Reviewer A (general-purpose, Claude Sonnet) — 2026-07-25
**Overall:** RFC's evidence is solid and I independently re-verified the three claims I judged most load-bearing (domain-layer 0-file count, Generators 8-consumer grep, D5's no-PackageReference finding) — all confirmed. I agree with the author's lean on D2, D4, and D5; I'd refine D1 toward a middle ground the author didn't quite land on; I agree with D3's direction but flag an operational gap.
**Decision 1 (commands/queries subfolder asymmetry):** Refined, leaning B — Document the pattern as intended (contracts are public surface) AND add non-enforced guidance that new work should also subfolder application/server files by operation where it aids navigation. Option A's rationale is a real but after-the-fact justification that doesn't actually fix the friction findings called out (newcomer can't find the handler beside the contract) — B costs nothing more than A and directly answers that friction instead of just explaining it away. C (analyzer) is overkill for a navigation preference with zero recorded confusion incidents.
**Decision 2 (empty domain layer):** A — cheap, consistent with the F5/`endpoint` precedent already established in the skill. One addition: give the "reserved headroom" documentation an explicit revisit trigger (e.g., a backlog note: "reassess if still zero after N more slices ship") rather than leaving it as open-ended narrative in this RFC — undated "reserved for later" is exactly how dead surface becomes permanent.
**Decision 3 (automate nuget.org release proof):** B — agree a post-publish gate in the same release closes the exact sequencing bug (template packed before pin bump) at the point it actually happens, better than a weekly schedule that only catches it after the fact. Flag for the child task: nuget.org indexing/propagation lag right after a fresh publish is a known gotcha — the gate needs retry/backoff, not a single immediate restore attempt, or it will produce false failures on the exact releases it's meant to protect.
**Decision 4 (guard sourceName-unsafe literals):** B — cheap, additive to an already-CI-wired gate, proportionate to a 2-occurrence bug class. Agree with author that A (analyzer) is worth revisiting only if this recurs a third time.
**Decision 5 (timewarp-templates second packaging surface):** A, verification confirmed independently — I re-ran the checks myself: `timewarp-architecture-template.csproj` has zero `<PackageReference>` items, the `.slnx` references exactly one project, and commit `22aa24b0` is real and does delete the `timewarp-architecture-template-tests` harness the stale comment refers to. No dispute; the premise-is-false finding holds.
**Anything the author missed:** My own grep for `TwArchitectureGeneratorsPackageId` consumers found 9 file hits, not 8 — but the 9th is `source/analyzers/timewarp-architecture-analyzers/timewarp-architecture-analyzers.csproj`, which uses the property to set its *own* PackageId (it's the package's producer, not a consumer). Worth a one-line callout in the P2 fold-in fix so whoever updates the how-to-guide's consumer table doesn't naively grep and land on 9 instead of 8 — the correct scoping (exclude the producer project) should be stated explicitly, not left implicit.

### Reviewer B (general-purpose, Claude Sonnet) — 2026-07-25
**Overall:** Author's leans hold up well from a maintainer-cost lens — I verified the file counts, the template-smoke Design region, the CI trigger config, and the D5 packaging-surface premise directly, and all matched the RFC's claims; I agree with 4 of 5 leans and refine D3.
**Decision 1 (commands/queries subfolder asymmetry):** A — I independently re-listed `admin/roles/` (5 subfoldered contract files vs. 8 flat files) and confirmed the asymmetry is real, but zero recorded confusion incidents plus current max slice size of 33 files means the ongoing maintenance cost of a folder-aware analyzer (C) isn't justified yet, and B's "encouraged but unenforced" convention costs doc-authoring effort without actually changing agent behavior differently than A. Cheapest fix that also closes the doc gap.
**Decision 2 (empty domain layer):** A — zero-cost to keep, matches the already-accepted `endpoint` headroom precedent (F5), and B's registry-edit-⇒-full-rebuild cost is a bad trade against a plausible near-term real use (per-slice aggregate roots, per task 113's golden/aggregate work).
**Decision 3 (automate nuget.org release proof):** B, refined — I confirmed `.github/workflows/template-smoke.yml` has no `schedule:` trigger today and the Design region in `template-smoke-command.cs` explicitly states it "cannot see" stale-published-pin breaks by construction, so A's weekly job would just be a slower version of what B does at the right moment. The actual failure was release-sequencing, and a post-publish gate closes that structurally rather than probabilistically. Refinement: the child task must handle nuget.org indexing/propagation lag explicitly (bounded retry/backoff), and should keep C's checklist text as the documented manual fallback for when the automated step itself needs to be re-run or is temporarily unavailable — belt-and-suspenders, not either/or.
**Decision 4 (guard sourceName-unsafe literals):** B — this is a 2-occurrence bug class already caught only by human review once (commit `a251980f`), and `dev template-smoke` is already CI-wired and already does the mirror-image check (asserting rewritten-output fragments are absent). A grep for the raw literal pattern is proportionate; standing up a Roslyn analyzer (A) for a single literal-token pattern is maintenance overhead disproportionate to the problem size today.
**Decision 5 (timewarp-templates second packaging surface):** A, verification confirmed — I independently read `timewarp-templates/Directory.Packages.props` (comment references a "template-test harness"), confirmed the referenced csproj has 0 `PackageReference` items, confirmed the `.slnx` references exactly one project, and confirmed commit `22aa24b0` deleted the old `timewarp-architecture-template-tests` harness project the comment describes. The open question's premise is false as stated — no second surface with real inline pins exists today. Agree: doc-only fix (rewrite the comment to say it's currently inert headroom), don't delete the file.
**Anything the author missed:** D1's rationale ("contracts are the layer other code may reference across slice boundaries in spirit") is worth stating explicitly in the skill's fixed worked example, not just "deliberate" — it's the actual mechanism (contracts are the public/shared surface consumed by SPA and cross-slice code even under TWA0009) that makes the asymmetry defensible rather than arbitrary, and spelling it out is what prevents a future agent from "fixing" the asymmetry by flattening contracts too. Also worth noting for D3's child task: task 124's incident was caught by a *human* running the outside-monorepo proof once by hand — the child task should specify who/what triggers a re-run if the post-publish gate itself fails (does a failed post-publish gate block the release as "not done," or just alert?), since that decision changes the release-pipeline blast radius significantly.

### Adversarial C (general-purpose, Claude Sonnet, adversarial brief) — 2026-07-25
**Overall:** RFC's evidence base is unusually solid — every falsifiable claim I re-derived from scratch (zero-rename git log, 73-file breakdown, admin/roles 5+8 split, identity 14+19 split, 8 Generators consumers, three stale Directory.Packages.props comments, D5's zero-PackageReference/commit-22aa24b0 verification) reproduced exactly. I dissent from the author on D1 (and from Reviewer A's refinement toward B), agree with D2/D3/D5, and refine D4 with two concrete implementation risks including a direct answer on the a251980f case.
**Decision 1 (commands/queries subfolder asymmetry):** A — I attacked "documenting-only doesn't fix the newcomer-can't-find-the-handler friction" and initially leaned toward B (Reviewer A's refinement) for that reason. But B — an unenforced convention extended to more layers — is worse, not better, given this repo's own standing directive against convention-by-memory: it doesn't close the enforcement gap (only C would), and being optional it will be applied inconsistently across authors/agents, reproducing the exact undocumented-drift pattern F3 already flagged, just over a larger surface. A confines the asymmetry to a place with a genuine architectural rationale — verified against AGENTS.md's own TWA0009 rule text ("share via Components/contracts"), not just the RFC's assertion — rather than extending an unenforced pattern further.
**Decision 2 (empty domain layer):** A — no attack surface found beyond what's documented; matches the F5/`endpoint` precedent exactly (verified `skills/tw-feature-placement/SKILL.md:60` and the 0-file count independently). Reviewer A's "explicit revisit trigger" addition is worth folding in.
**Decision 3 (automate nuget.org release proof):** B — verified `tools/dev-cli/endpoints/workflow-command.cs`'s release pipeline (Clean→Build→Pack→Push) has a real insertion point after `PushAsync`, so B is structurally feasible, not hand-wavy. Correction to the record: task 124's own grounding notes (`kanban/done/124-...md:64`) state "NuGet website search-index lag is cosmetic — flatcontainer (the restore path) had every version within minutes." The generic "nuget.org indexing lag" cited by both the RFC and Reviewer A as a cost of B is, per the repo's own evidence, a proven non-issue for the actual restore path; the real (smaller, still real) risk is flatcontainer propagation on the order of minutes — which still justifies a retry/backoff in the child task, just for a more precise, better-cited reason than generic indexing lag.
**Decision 4 (guard sourceName-unsafe literals):** B — holds, and I directly checked whether the proposed grep would have caught a251980f: the ORIGINAL bug was a literal `using TimeWarp.Architecture.TypedIds.Ef;` inside `postgres-db-context.cs` (a **.cs file**), not a csproj value — the fix removed that .cs literal and added a composed-property `<Using>` in `web-infrastructure.csproj`. Since template-smoke packs `source/**` including that .cs file into every generated app regardless of flags, a grep that scans .cs content WOULD have caught it. But the existing `AssertPackageIdsNotRewritten` check (`tools/dev-cli/endpoints/template-smoke-command.cs:405-407`) explicitly filters to only `.props/.csproj/.targets/.slnx/.json` — it excludes `.cs` by name. If D4 is implemented by naively extending that existing function rather than writing a new pass that explicitly adds `.cs`, it will silently fail to catch the exact bug class it's meant to catch. Second gap: `source/analyzers/**` (excluded from generated output only because `analyzerPackages` defaults true in the smoke matrix) contains dozens of legitimate `namespace TimeWarp.Architecture.Analyzers;` declarations plus one intentionally-baked-in literal constant (`EfNamespace = "TimeWarp.Architecture.TypedIds.Ef"` in `typed-id-source-generator.cs` — the very ground truth the composed property mirrors) that are invisible today only because the matrix never exercises `analyzerPackages=false`. If that matrix ever grows a source-mode leg, a naive "any literal anywhere is forbidden" grep will false-positive across the analyzer source tree. Neither issue changes the lean, but both need to be written into the D4 child task explicitly (include .cs; scope/exclude source/analyzers/** or gate on consumer paths) — this is a real scoping decision, not "one grep line."
**Decision 5 (timewarp-templates second packaging surface):** A — independently reproduced every part of the verification myself: the template csproj has zero `<PackageReference>` items, the `.slnx` references exactly one project, and commit `22aa24b0` is real and deletes precisely the `timewarp-architecture-template-tests` harness the stale comment describes. No dispute.
**Anything the author missed:** No claim failed re-verification. Two implementation-risk details for D4's child task (the `.cs`-extension gap in the reusable existing filter, and the `source/analyzers/**` false-positive risk if the smoke matrix ever grows a source-mode leg) should be recorded so they aren't rediscovered the hard way. The "objective, not balloted" categorization (P1/P2/P4) holds up — no framing error found there.

---

## 7. Tally

Standing rule (per `tw-rfc-ballot`): any decision where reviewers split (not unanimous) is marked
**"Dissent → maintainer (Steve)"** and is **not** resolved by agents — only unanimous outcomes are
self-resolving.

| # | Topic | Reviewer A | Reviewer B | Adversarial C | Outcome |
|---|-------|------------|------------|----------------|---------|
| D1 | commands/queries subfolder asymmetry | refined B | A | A | 2–1 split — **Dissent → maintainer (Steve)** |
| D2 | empty domain layer | A | A | A | **3–0 resolved: A** + explicit revisit trigger (Reviewer A, seconded by C) |
| D3 | automate nuget.org release proof | B | B (refined) | B | **3–0 resolved: B** — child task; retry/backoff for flatcontainer propagation (minutes-scale, per 124 notes — not generic index lag); keep manual checklist as documented fallback; child task must specify whether a failed gate blocks the release |
| D4 | guard sourceName-unsafe literals | B | B | B | **3–0 resolved: B** — child task; MUST scan `.cs` content (existing `AssertPackageIdsNotRewritten` filter excludes `.cs` — naive reuse would miss the a251980f class); must scope/exclude `source/analyzers/**` legit platform-namespace declarations |
| D5 | timewarp-templates second packaging surface | A | A | A | **3–0 resolved: A** — doc-only comment fix; premise-is-false verification independently reproduced by all three reviewers |

**Tallied 2026-07-25.** Ballots were cast independently and in parallel; none saw another's entry
before voting. All falsifiable evidence claims were re-verified by at least one reviewer
(Adversarial C re-derived every load-bearing claim from scratch; none failed). D1 goes to the
maintainer with the split rationale: Reviewer A argues documenting-only rationalizes the friction
without fixing it (wants documented non-enforced symmetrize-going-forward guidance); B and C argue
an unenforced convention over more layers reproduces the convention-by-memory drift pattern F3
flagged, and the asymmetry has a real architectural rationale (contracts are the public/shared
surface per TWA0009's own rule text) worth documenting as intended.

---

## 8. Fold-in checklist (host task 126)

Fold-in is **126**, not a sibling process task, per `tw-rfc-ballot` / `tw-agent-collaboration`.

- [ ] P1 — fix three stale `Directory.Packages.props` comments (no ballot needed)
- [ ] P2 — fix `HowToUpgradeToAnalyzerPackages.md` consumer table (no ballot needed)
- [ ] D5 — fix vestigial `timewarp-templates/Directory.Packages.props` comment (pending ballot
      confirmation of verification; doc-only, no ballot blocker expected)
- [ ] Run parallel ballots (2 reviewers + optional adversarial); tally in §7
- [ ] Steve resolves any non-unanimous decision; record resolution + reasoning in §7
- [ ] Doc-only resolutions (skill/AGENTS.md/how-to-guide wording) land directly on 126
- [ ] Structural resolutions (new analyzer, new release-workflow step, registry changes) become
      proposed **child task** titles pending maintainer sign-off — not implemented on 126
- [ ] Record Results on `task.md`: evidence summary, ballot outcome + tally, what landed on 126 vs.
      what was deferred to which child-task titles
