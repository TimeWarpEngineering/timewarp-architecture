# Add post-publish release gate: generate published template against real nuget.org

## Description

From task 126 RFC Decision 3 (ballot unanimous for a release-workflow post-publish step;
maintainer confirmed 2026-07-26 with **block semantics**): automate the proof task 124 performed
once by hand, as a required step immediately after each release publishes.

**The gap it closes:** `dev template-smoke` packs this branch's packages into a local feed at
`2.0.0-smoke` and generates against that — it proves branch-internal consistency and structurally
cannot see stale/broken published pins (its own Design region,
`tools/dev-cli/endpoints/template-smoke-command.cs:11-15`, says so). The beta.5/beta.6 breakage
(generated apps hit CS0234 + `TimeWarp.Identity` 404 from nuget.org) shipped to real users while
template-smoke stayed green. Task 124's pins==release-version policy closes the systematic drift,
but the choreography is human-enforced and was fumbled once already (beta.6 template packed
before the pin bump — two releases in one day).

**The gate:** after packages + template push in the release run:

1. Wait/retry for nuget.org flatcontainer availability of the just-published versions —
   **bounded retry with backoff**, minutes-scale. Per task 124's own notes, flatcontainer (the
   restore path) had every version "within minutes"; website search-index lag is cosmetic and
   NOT what to wait on.
2. In a clean environment (fresh `dotnet new` hive, no local feeds, no monorepo nuget.config):
   install the just-published template version from nuget.org, `dotnet new
   timewarp-architecture` (app name ≠ sourceName so rewrite bugs also surface), restore against
   nuget.org ONLY, build.
3. **Failure blocks the release** (maintainer decision): the release is not done until this is
   green. Rationale: a failure here means something broken is already published — the honest
   state is a red release, and the correct response is what 124 did (fix, ship next beta
   immediately). A nuget.org hiccup wedging a release until retry is an accepted cost.

## Checklist

- [x] Locate insertion point in the release path (`tools/dev-cli/endpoints/workflow-command.cs`
      Clean→Build→Pack→Push — verified insertion point exists after `PushAsync`; wire through
      `.github/workflows/workflow.yml` release mode)
- [x] Implement clean-hive generate + nuget.org-only restore + build, with bounded
      flatcontainer retry/backoff
- [x] Failure blocks release completion (exit nonzero in release mode; no test-gate softening)
- [x] Consider default matrix (at minimum default flags; optionally reuse template-smoke's
      two-matrix shape if cheap)
- [x] Document the manual fallback (the exact 124 steps) in the release how-to/checklist so the
      proof can be run by hand when the automated step is unavailable
- [x] Prove the gate catches the real failure class: dry-run it against a known-good published
      version, and verify it WOULD fail on a synthetic stale-pin scenario if cheaply testable
- [x] Leave `dev template-smoke` untouched — complementary coverage (branch-internal vs
      published-reality), not replaced

## Notes

- Parent: 126. Ballot record: `126 rfc/rfc.md` Decision 3 — unanimous B (A/B/C reviewers), with
  refinements folded in above (Reviewer A: retry/backoff; Reviewer B: keep manual fallback +
  decide block-vs-alert; Adversarial C: flatcontainer-not-search-index precision, verified
  insertion point). Maintainer resolved block-vs-alert to **block** (2026-07-26).
- CI pattern constraint (repo convention): single `workflow.yml`, mode-aware `dev workflow`;
  release publishes via OIDC with no test gate — this gate is a release-integrity check, not a
  test gate; mirror sibling-repo patterns rather than inventing new workflow shapes.
- Related but distinct: 126-004 candidate (sourceName-unsafe literal grep inside template-smoke)
  guards a different failure class (source-side literals) — do not merge the two.
- Review kitchen: `review/` (framework, round-1 general + merged, disposition clean).

## Implementation Plan (2026-07-27)

### Goal
Required post-publish gate: after real nuget.org push, wait for flatcontainer, install published template in a clean hive, generate (name ≠ sourceName), assert CPM pins == release version, restore nuget.org-only, build. Failure blocks release. Leave `dev template-smoke` untouched.

### Design
1. **New command** `template-publish-smoke` at `tools/dev-cli/endpoints/template-publish-smoke-command.cs`
   - `--version` (default: parse `source/Directory.Build.props` `<Version>`)
   - `--skip-wait` for re-runs when packages already available
   - Matrix: PublishSmokeDefault + PublishSmokeNoPostgres (same as template-smoke)
   - Flatcontainer wait: all platform packages + template; exponential backoff; 12 min budget
   - Isolation: `artifacts/template-publish-smoke/{cli-home,nuget-packages,work}` via DOTNET_CLI_HOME + NUGET_PACKAGES
   - App-local NuGet.config: clear + nuget.org only
   - Pin assert + always-on synthetic stale-pin self-check
   - `git init` before build (task 124)
2. **Wire** `workflow-command.cs` RunReleaseAsync: after successful PushAsync **and** API key present → invoke gate via RunStepAsync. Pack-only skips.
3. **Docs** `HowToRelease.md` + Overview link; workflow.yml header comment only (no new job).
4. **Verify** dry-run against 2.0.0-beta.7; pack-only skip; dev build 0/0; template-smoke still green.

### Non-goals
Do not modify template-smoke; no second workflow job; no soft-fail; no pin rewrite.

## Session

- Created: 2026-07-26 — filed from 126 RFC Decision 3 + maintainer block-semantics resolution.
- Orchestrator / implement: grok-build 2026-07-27
- Review: general round-1 2026-07-27

## Results

### What was implemented
Post-publish release gate (`dev template-publish-smoke`) that, after a real nuget.org push in
`dev workflow` release mode, waits for flatcontainer availability, installs the published
template into an isolated CLI hive, generates apps (defaults + `--postgres false`, names ≠
sourceName), asserts CPM platform pins equal the release version, restore+builds against
nuget.org only. Failure sets nonzero exit and blocks Pipeline SUCCEEDED. Pack-only (no API key)
skips the gate.

### Files changed
| Path | Action |
|------|--------|
| `tools/dev-cli/endpoints/template-publish-smoke-command.cs` | Created |
| `tools/dev-cli/endpoints/workflow-command.cs` | Wire gate after real push; Design region |
| `documentation/developer/how-to-guides/HowToRelease.md` | Created — automated path + task 124 manual fallback |
| `documentation/developer/how-to-guides/Overview.md` | Link |
| `.github/workflows/workflow.yml` | Header comment only (no new job) |

### Key decisions / deviations
1. **`git init` + empty commit** (not init alone) — TimeWarp.Build.Tasks needs a real HEAD.
2. Install uses `@` separator (`TimeWarp.Architecture@{version}`), not deprecated `::`.
3. Always-on pin-assert self-check (correct / stale / zero-pin) proves beta.6 failure class without publishing bad packages.
4. `template-smoke` left complementary and unmodified.

### Test outcomes
| Check | Result |
|-------|--------|
| `dotnet run tools/dev-cli/dev.cs -- template-publish-smoke --help` | Pass |
| `--version 2.0.0-beta.7 --skip-wait` dry-run | Pass — both matrix apps 0/0; pins == beta.7 |
| Pin self-check (stale / zero) | Pass (always-on) |
| Pack-only skip | Code path: `willPublish` false → no gate |

### Review (Phase 4b)
- **Rounds:** 1
- **Effort / roster:** 1 — general only
- **Final counts:** 0 open / 0 fixed / 0 wontfix (all severities)
- **Disposition:** `clean` — `review/disposition.md`
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
