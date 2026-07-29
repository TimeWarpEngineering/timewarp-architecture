# Complete repo code review by Kimi K3

## Description

Hold a **complete repository code review** performed by **Kimi K3**, using the
strict maintainability bar from the TimeWarp `/code-review` skill (abstraction
quality, giant files, spaghetti-condition growth, and ambitious "code judo"
restructuring).

This is **not** a PR-diff review. Scope is the full monorepo as of the review
commit: template content under `source/` + `tests/`, platform packages
(foundation, analyzers/generators, identity), Aspire orchestration, packaging
(`timewarp-templates/`), skills/docs that encode conventions, and MSBuild dual-mode
wiring. Generated/obj/bin noise is out of scope.

The folder task is the durable home for the review brief, findings report,
disposition notes, and any follow-on task links.

## Requirements

### Reviewer and method

- Reviewer model: **Kimi K3** (run in a session that can read the whole tree).
- Apply the `/code-review` skill baseline **adapted to full-repo scope** (not
  "current branch's changes"):

  > Perform a deep code quality audit of the repository.
  > Rethink how to structure / implement areas to meaningfully improve code quality
  > without impacting behavior.
  > Work to improve abstractions, modularity, reduce spaghetti code, improve
  > succinctness and legibility.
  > Be ambitious: if there is a clear path to improving the implementation that
  > involves restructuring some of the codebase, go for it.
  > Be extremely thorough and rigorous. Measure twice, cut once.

- Plus the skill's non-negotiable standards (file size past ~1k lines, spaghetti
  growth, type/boundary cleanliness, canonical-layer placement, unnecessary
  orchestration, thin wrappers / magic abstractions, missed code-judo moves).

### Coverage (must walk)

| Area | Path hints |
|------|------------|
| Web family (features + platform + projects) | `source/container-apps/web/` |
| API / gRPC / YARP / Aspire | `source/container-apps/{api,grpc,yarp,aspire}/` |
| Foundation packages | `source/foundation/` |
| Analyzers + generators | `source/analyzers/` |
| Identity library | `source/libraries/timewarp-identity/` |
| Tests (Fixie/Shouldly patterns) | `tests/` |
| Template packaging / preprocessor flags | `timewarp-templates/`, `.template.config/`, `<!--#if` / `#if flag` |
| Convention enforcement (TWA*) | analyzers, `AGENTS.md`, skills under `skills/` / flow skills |
| Build / dual-mode MSBuild | root `Directory.Build.*`, `Directory.Packages.props`, `msbuild/` |

### Finding quality bar

Prioritize findings in this order (from `/code-review`):

1. Structural code-quality regressions / architecture drift
2. Missed opportunities for dramatic simplification (code judo)
3. Spaghetti / branching complexity
4. Boundary / abstraction / type-contract problems
5. File-size and decomposition concerns
6. Modularity and abstraction issues
7. Legibility and maintainability concerns

**Do not flood** with low-value nits when larger structural issues exist.
Prefer a smaller set of high-conviction comments over cosmetic lists.

**False positives to suppress** (same spirit as PR review skill):

- Issues a linter/typechecker/compiler/CI would catch
- Pedantic style / formatting
- Pre-existing issues that are already tracked in kanban (link them instead)
- "Works but less pretty" without a concrete simpler structure
- Demo/template intentional scaffolding called out in HowToRemoveDemoFeatures.md

### Repo-specific lenses (TimeWarp Architecture)

In addition to generic maintainability, the review **must** check alignment with:

- **Slice isolation (TWA0009)** — product slices don't reach other product slices
- **Feature placement / filename grammar (TWA0015/0016)** — features vs platform trees
- **Endpoint-centric contracts** — `[ApiEndpoint]` + exactly one of
  `[EndpointAuthorize]` / `[EndpointAllowAnonymous]`; FastEndpoints generated from
  contracts; validation on mediator `FluentValidationBehavior` only
- **TimeWarp.Mediator** (not MediatR) request shapes
- **Aggregate pattern (TWA0011/0012)** — private nested `Invariants`
- **Package dual-mode** — foundation / analyzers / identity ProjectReference vs
  published package IDs; sourceName-safe package ID composition
- **Template flag regions** — no silent `#if` / `<!--#if` breakage (TWA0008/0010)
- **Agent context regions** — Purpose/Design honest and current (TWA0004)
- **Tests** — Fixie + Shouldly; no FluentAssertions; contracts serialization tests
  where non-trivial

### Deliverables (in this folder)

1. **`review-brief.md`** — commit SHA, date, model, scope exclusions, how the
   tree was walked
2. **`findings.md`** — ordered findings; each finding has:
   - Severity: `blocker` | `major` | `minor` | `note`
   - Area / path (file:line when possible)
   - What is wrong
   - Why it matters (maintainability / cascade risk)
   - Preferred remedy (prefer delete/reframe over polish)
   - Suggested follow-on kanban task title if work is multi-file
3. **`disposition.md`** — human/steward response: accept / defer / reject per
   finding, with links to child tasks created from accepted items
4. Optional: area deep-dives (`findings-web.md`, `findings-analyzers.md`, …)
   if a single file becomes unreadable

### Done criteria

- Review artifacts committed under this folder
- Every **blocker** and **major** finding has a disposition
- Accepted work is either fixed under this task or split into numbered child /
  sibling kanban tasks (via `ganda kanban create`, never hand-numbered)
- No silent "looks fine" rubber stamp: if approval is recommended, the approval
  bar from `/code-review` is explicitly argued

## Checklist

### Prep
- [x] Pin review commit SHA and record in `review-brief.md`
- [x] Confirm reviewer is Kimi K3; note session id under Session
- [x] Read root `AGENTS.md` + relevant skills (`tw-csharp`, `tw-feature-placement`,
      `tw-slice-isolation`, `tw-web-api-contracts`, `tw-aggregate-pattern`,
      `agent-context-regions`)

### Review execution (Kimi K3)
- [x] Walk web / api / grpc / yarp / aspire container-apps
- [x] Walk foundation + identity library
- [x] Walk analyzers / generators and TWA rule surface
- [x] Walk tests for pattern drift (Fixie/Shouldly, host-free contract tests)
- [x] Walk template packaging and preprocessor / dual-mode MSBuild
- [x] Apply ambitious code-judo lens (delete complexity, not just rearrange)
- [x] Flag files approaching or past ~1000 lines and unjustified growth risk
- [x] Flag spaghetti growth, boundary leaks, thin wrappers, wrong-layer logic
- [x] Cross-check high findings against existing kanban (avoid duplicate tasks)

### Artifacts and disposition
- [x] Write `review-brief.md`
- [x] Write `findings.md` (high-conviction, prioritized)
- [x] Steward disposition on blocker/major items → `disposition.md`
- [x] Create follow-on kanban tasks for accepted multi-step work (131-001…131-004)
- [x] Implement accepted under-131 fixes (F-001/002/009–017)
- [ ] Mark this task done when Results accepted and steward moves column

## Notes

- Skill SSOT: TimeWarp `/code-review` skill
  (`timewarp-flow` → `grok/skills/code-review/SKILL.md` or local equivalent).
  That skill is written for PR diffs; this task **reuses its standards and
  approval bar** for a **whole-repo** audit.
- Repo is the `dotnet new timewarp-architecture` template; findings that would
  ship into every generated app rank higher than dogfood-only local conveniences.
- Suggested first follow-on pattern after disposition: one folder or parent task
  per theme (e.g. "decompose X", "collapse dual path Y") rather than one task per
  nit.
- Do **not** run full `dev build` / `dev test` as part of the review itself unless
  a finding requires verification; CI owns green builds. Review is structural and
  design-focused.
- Large source files at task creation (informational, non-obj): typed-id generator
  (~554), feature-filename-grammar analyzer (~476), web-server Program (~368),
  ef-principal-store (~347), ingress-route-prefix generator (~389). None over 1k
  yet; watch generators and host Programs.

### Execution plan (Phase 2, 2026-07-28)

- Review commit pinned: `2b5dc765` (HEAD at execution start).
- Judgment/findings rendered by Kimi K3 main session only; sub-agents run mechanical inventories only.
- Out of scope: obj/bin, full dev build/test battery, program-104 future surface, settled architecture (109, 114/126/129) unless drift visible.

**Walk order (ship-risk first):**
- W0 conventions SSOT (AGENTS.md, TWA table) → W1 dual-mode MSBuild + template packaging + dev-cli smoke commands (deep) → W2 analyzers + generators (deep) → W3 foundation (deep seams, scan leaves) → W4 identity library (deep ceremonies/stores) → W5 web family (deep platform/identity-host/host Programs + identity/profile slices; golden-path one demo slice; structural rest) → W6 api/grpc/yarp/aspire (structural + host deep) → W7 tests (pattern audit) → W8 cross-cutting synthesis.

**Phase A fan-out (6 parallel explore jobs):** A1 file-size/hotspot inventory; A2 convention/anti-pattern grep pack (MediatR leftovers, FluentAssertions, auth-marker pairing, CrossSliceReference, Purpose regions, template flags, dual-mode symbols); A3 slice/placement/grammar scan; A4 contract/endpoint census ([ApiRoute]/[ApiEndpoint]/auth marker/Validator matrix + hand-written ingress list); A5 duplication/thin-wrapper candidates; A6 kanban dedup theme index (incl. done 126-review disposition status).

**Dedup rule:** open task covers it → `note` with `tracked: #N`, no new child. Done task but code still wrong → regression finding, major+. Program 104 cluster = future surface, only flag today's structural mess.

**Triage bar:** target 12–25 findings total, 6–12 blocker/major; every finding answers "delete/reframe what?"; collapse related nits into thematic findings; template-ship impact beats dogfood-only convenience.

**Artifacts under task folder:** review-brief.md (SHA, scope, walk truthfully recorded, sub-agent returns summarized), findings.md (ordered F-001… with severity/area/path/wrong/why/remedy/follow-on + theme summary), disposition.md (steward: accept/defer/reject per blocker/major + child links), optional per-area deep-dives if findings.md > ~400 lines. Sub-agent raw returns folded into brief then discarded (no scratch noise committed).

**Phases:** A mechanical scans (parallel agents) → B deep reads W0–W7 (checkpoints after W2 and W5) → C synthesis + artifacts → D steward disposition + `ganda kanban create` for accepted themes (one parent per theme, not per nit) → Results → done.

## Session

- Created: (this session) 2026-07-28 — task scaffolding only
- Review: 2026-07-28 — Kimi K3 (orchestrator session, opencode) — complete
- Verification: 2026-07-28 — Claude + Grok independent re-verification (round-1)
- Disposition + implement: 2026-07-28 — steward walk + Grok implement (F-001…F-017 under-131 set)

## Results

Full-repo maintainability review (17 findings) dispositioned and partially implemented.

**Implemented on 131:** F-001 (Azure App Config out of foundation), F-002 (MVC BaseEndpoint
deleted, TWA0005 retired), F-009 (MOCK_AUTHENTICATION all configs), F-010 fossils + MediatR
link, F-011 postgres exclude glob, F-012 tests DBP detection, F-013 grammar simplify, F-015
SPA transport catch/verbs, F-016 Features substrate docs, F-017 residue sweep.

**Children (to-do):**
- **131-001** generator/analyzer hardening — F-003, F-004, F-005, F-008, F-014
- **131-002** identity problem/ceremony de-dup — F-006
- **131-003** template-smoke harness SSOT — F-007
- **131-004** shared API transport core — F-015 extract

**Tracked elsewhere:** 104-016 (Passwordless CDN/key), 104-021 (Entra posture; notes updated).

**Review artifacts:** `review-brief.md`, `findings.md`, `disposition.md`,
`review/review-framework.md`, `review/round-1/{claude,grok}-verification.md`.