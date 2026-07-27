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

- [ ] Locate insertion point in the release path (`tools/dev-cli/endpoints/workflow-command.cs`
      Clean→Build→Pack→Push — verified insertion point exists after `PushAsync`; wire through
      `.github/workflows/workflow.yml` release mode)
- [ ] Implement clean-hive generate + nuget.org-only restore + build, with bounded
      flatcontainer retry/backoff
- [ ] Failure blocks release completion (exit nonzero in release mode; no test-gate softening)
- [ ] Consider default matrix (at minimum default flags; optionally reuse template-smoke's
      two-matrix shape if cheap)
- [ ] Document the manual fallback (the exact 124 steps) in the release how-to/checklist so the
      proof can be run by hand when the automated step is unavailable
- [ ] Prove the gate catches the real failure class: dry-run it against a known-good published
      version, and verify it WOULD fail on a synthetic stale-pin scenario if cheaply testable
- [ ] Leave `dev template-smoke` untouched — complementary coverage (branch-internal vs
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

## Session

- Created: 2026-07-26 — filed from 126 RFC Decision 3 + maintainer block-semantics resolution.
