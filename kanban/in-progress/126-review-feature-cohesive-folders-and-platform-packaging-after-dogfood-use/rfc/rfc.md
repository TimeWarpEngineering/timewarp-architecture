# RFC: Post-dogfood disposition for feature-cohesive folders + platform packaging

**Status:** draft — awaiting ballots.
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

---

## 7. Tally

Standing rule (per `tw-rfc-ballot`): any decision where reviewers split (not unanimous) is marked
**"Dissent → maintainer (Steve)"** and is **not** resolved by agents — only unanimous outcomes are
self-resolving.

| # | Topic | Reviewer A | Reviewer B | Adversarial C | Outcome |
|---|-------|------------|------------|----------------|---------|
| D1 | commands/queries subfolder asymmetry | | | | |
| D2 | empty domain layer | | | | |
| D3 | automate nuget.org release proof | | | | |
| D4 | guard sourceName-unsafe literals | | | | |
| D5 | timewarp-templates second packaging surface | | | | |

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
