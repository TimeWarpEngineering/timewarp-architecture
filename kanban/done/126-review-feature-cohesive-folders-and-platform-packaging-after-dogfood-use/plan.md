# Implementation Plan — Kanban Task 126: Review Feature-Cohesive Folders & Platform Packaging After Dogfood Use

All paths below were verified against the actual repo tree at the dev worktree root. The task's
own process table is authoritative — this plan operationalizes it, it does not replace it:
**Phase 1 evidence → Phase 2 tw-rfc-ballot → Phase 3 fold-in, same task id 126 throughout.**

---

## Phase 0 — rails (continuous, not a step)

Use `tw-agent-collaboration` conventions for the whole task: work happens in the existing folder
`kanban/in-progress/126-review-feature-cohesive-folders-and-platform-packaging-after-dogfood-use/`,
Notes/Session entries accumulate in `task.md`, Results goes in `task.md` only at the very end, and
every sub-artifact (`findings.md`, `rfc/`, optional `review/`/`debate/`) stays under this same task
folder — never spawn a sibling task for RFC or fold-in work.

---

## Phase 1 — Evidence

### Step 1.1 — Re-read baseline contract docs (do this first, verbatim, no summarizing from memory)

- `documentation/developer/conceptual/architectural-decision-records/approved/0008-feature-cohesive-folders-with-filename-grammar-layer-composition.md`
  — full ADR. Extract: the two chosen mechanisms (decouple disk layout from project membership;
  registry-driven grammar), and especially the **"Negative Consequences"** section (filenames carry
  grammar; registry edits require full rebuild; path-normalization pitfalls; wrong-folder files
  surface at build not at creation) — these are the ADR's own predicted frictions and Phase 1
  should check whether dogfooding confirmed them.
- `skills/tw-feature-placement/SKILL.md` — the grammar table, registry JSON shape, membership
  guard, TWA0015/TWA0016 semantics, SPA exception, axis-2 per-module-assembly-split note.
- `AGENTS.md` — two anchor sections: "Layout" (filename grammar summary) and "Platform packages
  (foundation + analyzers + identity)" (dual-mode packaging, CPM pin policy, sourceName-safe
  package IDs).

### Step 1.2 — Survey the live `web/features/` tree mechanically

```bash
# Slice list
find source/container-apps/web/features -maxdepth 1 -type d

# File count by layer suffix (grammar compliance check)
find source/container-apps/web/features -name "*.cs" \
  | sed -E 's/.*-(contracts|application|domain|infrastructure|server)\.cs$/\1/' \
  | sort | uniq -c

# Total files under the cohesive tree
find source/container-apps/web/features -name "*.cs" | wc -l

# Full listing to manually classify function-segment vs escape-hatch vs oddball
find source/container-apps/web/features -name "*.cs" | sort
```

Already-observed counts to compare against (re-run to confirm currency): 27 `application`,
37 `contracts`, 3 `infrastructure`, 6 `server`, **0 `domain`** — 73 files total. The zero
`domain` files is itself a finding candidate (worth asking in the RFC: is `domain` an
actively-used layer in product slices at all, or has every dogfooded feature only ever needed
application+contracts+server?).

Cross-check against the registry for compliance:

```bash
cat source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json
```

Registered functions today: `handler → application`, `endpoint → server` (reserved, unused),
`feature-annotations → server`. For every file found, classify: (a) uses a registered function
correctly, (b) escape hatch (`<name>-<layer>.cs`, no function segment — e.g.
`role-store-application.cs`, `chat-hub-constants-contracts.cs`), (c) anything that would trip
TWA0015/TWA0016 today (there should be none live, since the build enforces it — but check for
near-miss names in git history of renames).

Count escape-hatch usage explicitly vs archetype usage — this ratio is a direct signal for the
RFC ("is the registry too small — should more archetypes like `store` be registered, given how
many files fall to the escape hatch?").

Also check the SPA exception is holding:

```bash
find source/container-apps/web/web-spa/features -maxdepth 1 -type d
```

Confirm none of the SPA slice folders have acquired filename-grammar suffixes by accident
(spot-check a few `.razor`/`.cs` filenames).

### Step 1.3 — Survey the packaging surface

Read/inspect these exact files:
- `msbuild/timewarp-platform-packages.props` — the sourceName-safe property composition
  (`TwArchitectureAnalyzersPackageId`, etc.)
- `Directory.Packages.props` (root) — the platform pins block. **Concrete drift already found and
  worth flagging in findings.md**: the comment above the `TimeWarp.Identity` pin still says
  *"TimeWarp.Identity has never been published..."* This predates task 124, which shipped the
  beta.6 first publish of `TimeWarp.Identity` and flipped `identityPackages` default to `true`.
  Stale-comment / doc-drift finding — a good "trivial fix, fold in directly" candidate for Phase 3.
- `Directory.Build.props` (root) — where `UseFoundationPackages`/`UseAnalyzerPackages`/
  `UseIdentityPackages` dual-mode switches are defined (auto-detect missing source trees).
- `.template.config/template.json` — `foundationPackages`/`analyzerPackages`/`identityPackages`
  symbols and their default values/descriptions.
- `timewarp-templates/` tree — the NuGet packaging tree that ships the template itself; check for
  any residual/duplicated packaging logic that could drift from root
  `msbuild/timewarp-platform-packages.props`.
- `.github/workflows/template-smoke.yml` and the `dev template-smoke` command — the regression
  gate for dual-mode; note whether it's actually being run/trusted in recent dogfood cycles.

Cross-reference the package table in `AGENTS.md` against what's actually pinned in
`Directory.Packages.props` to confirm the Analyzers/Generators split is real at the package level
(two separate NuGet IDs) and ask in the RFC whether that split has caused any attach-surface
confusion in dogfood use.

### Step 1.4 — Mine dogfood history for recorded friction

Task folders (all verified to exist):
- `kanban/done/113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation/`
  — read `task.md` Notes/Results and `review/disposition.md` for anything about feature-folder
  placement or packaging friction during golden persistence work.
- `kanban/done/104-032-implement-ef-core-persistence-for-identity-principal-store-behind-postgres-flag/`
  — the identity-EF dogfood; check for filename-grammar or dual-mode packaging friction notes.
- `kanban/done/124-release-200-beta6-republish-platform-packages-with-aggregatedbcontext-first-publish-identity-flip-identitypackages.md`
  — single-file task. Read fully — this is the CPM pin/release-choreography policy source and
  records the beta.6→beta.7 publish cycle friction directly.
- `kanban/done/115-fix-template-sourcename-rewriting-timewarparchitecture-package-ids-and-unpublished-timewarpidentity-reference/`
  — where the sourceName-safe package ID composition came from; read for the original bug/friction
  that motivated it.
- `kanban/done/092-publish-analyzers-and-source-generators-as-nuget-packages.md` and
  `kanban/done/051-create-timewarpfoundation-nuget-packages-from-common-layers.md` — foundational
  packaging lineage; skim for original design rationale to check current use against original intent.
- `kanban/done/114-architecture-direction-study-vertical-slice-vs-clean-architecture-reference-repo-survey-and-rfc/`
  and `kanban/done/114-002-migrate-web-slices-to-feature-cohesive-folders-with-filename-grammar-globs-and-shipped-registry/`
  — the origin of ADR-0008 and the migration; check `axis-decisions.md` if present, so Phase 2
  doesn't accidentally re-litigate an axis without new evidence.

Also run git-log mining over those task folders and `--grep "task 124"` for the actual commit
sequence — commit messages like "beta.7 ships correct pins; published-template proof 0/0" are
evidence of the pin/publish choreography working as designed (a "win" to cite), not just friction.

### Step 1.5 — Walk 2–3 recent dogfood features for hesitation/misplacement signals

Pick concretely: identity (`source/container-apps/web/features/identity/`), profile
(`source/container-apps/web/features/profile/`), and admin/roles
(`source/container-apps/web/features/admin/roles/`) — the most structurally rich slices (roles
has commands/, queries/, and escape-hatch file `role-store-application.cs`). For each: read the
file list, check the corresponding task's Notes for "renamed", "moved", "TWA0015", "TWA0016",
"membership guard" mentions, and note anything that looks like the grammar was learned by
trial-and-error rather than looked up in the skill.

```bash
grep -rn "TWA0015\|TWA0016\|membership guard\|feature-filename-grammar" kanban/done/104-* kanban/done/113-* --include="*.md"
```

### Step 1.6 — Write `findings.md`

Location: task folder root. Structure:

```markdown
# Findings — 126 post-dogfood review

## 1. Feature-cohesive folders + filename grammar
### Wins
### Frictions (with concrete file/count evidence from 1.2/1.5)
### Risks
### Open questions

## 2. Platform packaging dual-mode
### Wins
### Frictions (with concrete file evidence from 1.3/1.4, incl. the Directory.Packages.props stale-comment drift)
### Risks
### Open questions

## 3. Numbered candidate decisions (feed into rfc.md)
Each tagged keep / tweak / restructure-candidate, one line each, to be expanded into the RFC.
```

Every claim in `findings.md` must cite a real path/count/commit — this is what the RFC's evidence
matrix will draw from, and it's what a ballot reviewer verifies against.

---

## Phase 2 — RFC ballot (`tw-rfc-ballot` + `tw-agent-collaboration`)

### Step 2.1 — Create `rfc/rfc.md`

Follow the shape already proven in this repo at
`kanban/done/104-002-implement-principal-credential-and-trusttier-domain-model/rfc/rfc.md`
(read it as a template — do not invent a different rfc.md shape):

1. **Header**: Status (draft → tallied → folded in), host task (126), author, audience note.
2. **§1 Why this exists** — one paragraph pointing at `findings.md`; explicit "out of scope" list
   (do not re-litigate ADR-0008 axis choices unless `findings.md` produced *concrete* dogfood
   evidence against them).
3. **§2 Sources of truth (evidence matrix)** — table citing `findings.md` sections, the ADR, the
   skill, AGENTS.md, and the mined task files (113/104-032/115/124).
4. **§3 Objective / already-fixed** — uncontroversial bugs (e.g. the stale Identity-pin comment)
   noted as "not balloted, fix directly in fold-in."
5. **§4 Decisions needing ballots** — one subsection per numbered decision from `findings.md` §3,
   each with: Topic, Options table, Trade-offs, **Author lean** (explicit). Likely buckets:
   registry size/escape-hatch ratio, domain-layer usage in product slices, Analyzers/Generators
   split ergonomics, CPM pin/publish choreography follow-ups, dual-mode surprise points,
   docs/skill drift fixes.
6. **§5 Author priority** — ordering if leans are accepted.
7. **§6 Reviewer opinions** — empty, with the ballot template block at the bottom (mirror the
   104-002 template).
8. **§7 Tally** — populated after ballots; **any decision where reviewers disagree is marked
   "Dissent → maintainer" and left unresolved by agents.**
9. **§8 Fold-in checklist** — same host task 126: "Fold-in is 126, not a sibling process task."

### Step 2.2 — Reviewer count and composition

**2 independent parallel reviewers plus 1 optional adversarial reviewer** (matching the 104-002
precedent's `general-purpose-A`, `general-purpose-B`, `adversarial-C` pattern). Each reviewer gets
`rfc/rfc.md` plus the read-only evidence and returns an independent ballot entry appended under
§6, following the template exactly, without seeing other reviewers' entries first (parallel, not
sequential).

### Step 2.3 — Tally and maintainer resolution — explicit constraint

**For any decision where the reviewers split (not unanimous), the implementing agent must NOT
pick a side or average the opinions.** Mark it `Dissent → maintainer` and stop — present the
dissent with each reviewer's stated rationale to Steve for resolution. Only decisions with
unanimous ballots may be treated as resolved without maintainer input.

### Step 2.4 — Re-verify falsifiable claims

Before finalizing tally, spot-check any checkable reviewer claim (escape-hatch counts, zero
domain files, stale-comment claims) against actual repo state with the Step 1.2/1.3 commands.

---

## Phase 3 — Fold-in (same task id, 126)

### Step 3.1 — Disposition table

Classify every decision: **Keep** (no change, record why) / **Tweak** (small, uncontroversial,
lands directly on 126) / **Restructure candidate** (becomes a proposed child-task title, not
implemented on 126).

### Step 3.2 — What lands directly on 126 (fold-in criteria)

- Docs fix (ADR clarifying note, AGENTS.md wording, skill wording) — e.g. the stale
  `Directory.Packages.props` Identity-pin comment.
- Skill update (`skills/tw-feature-placement/SKILL.md`).
- Analyzer/generator message or diagnostic wording improvement (not new diagnostics).
- Registry addition of a new function entry *if and only if* the ballot unanimously supports it
  and evidence shows a repeated escape-hatch pattern — requires full rebuild (`dev build` clean)
  per the ADR's documented negative consequence; discrete, verified step.
- Trivial CPM/props/template-symbol wording or comment fixes that don't change generated-app
  behavior.

### Step 3.3 — What becomes a child task instead

Anything that would: rename/move product files, change the grammar's layer set, restructure
`msbuild/timewarp-platform-packages.props` composition, change which packages exist or are split,
alter the CPM pin policy itself, or touch `.template.config/template.json` symbol defaults.
Propose the child-task title in the disposition table; create it only with maintainer sign-off.

### Step 3.4 — Record Results on 126

`task.md` Results covers: evidence summary (link `findings.md`), ballot outcome (link `rfc/rfc.md`
tally + maintainer resolutions), and an explicit list of what landed on 126 vs what was deferred
to which child-task titles.

---

## Optional switches (only if triggered — do not pre-plan their content)

- Concrete fix-now machinery defect found → open `review/` and run `tw-implementation-review` on
  that specific delta only.
- Ballots collapse to one hard architecture fork → open `debate/` and run `tw-consensus-debate`
  on that one question only.

---

## Risks / constraints (carry through every phase)

1. **Do not change production structure in this task** unless the fix is trivial and
   uncontroversial — non-trivial structural change is always a child task.
2. **Do not re-litigate ADR-0008's axis decisions** without concrete dogfood evidence.
3. **Registry edits require a full rebuild** — stale analyzer DLLs silently keep enforcing the
   old grammar under incremental builds.
4. **Steve resolves dissent** — no agent may pick a side on a split ballot. Only unanimous ballot
   outcomes are self-resolving.
5. Keep Phase 2 to the `tw-rfc-ballot` shape — do not drift into consensus-debate's sequential
   single-fork shape or implementation-review's diff/severity shape as the default engine.
