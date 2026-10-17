# CI: regenerate generated output and fail on any diff

## Description

Generated artifacts drift when a human or agent edits the output (or a non-deterministic generator) without re-running the source of truth. Borrow Anatomy's pattern: CI regenerates the output and fails the build on any difference (`git diff --exit-code`).

**Scope (only these two families):**

1. **Server-offer generation** from tasks 280–282 (`[Offerable]` / offer records emitted by the contracts generator).
2. **YARP ingress route generation** from task 107 (`IngressRoutePrefixGenerator` / web-contracts `[ApiRoute]` prefixes consumed by the AppHost).

Do not expand to every generator in the repo.

## Requirements

1. **Determinism first.** Before wiring CI, make both generators produce byte-stable output across runs on the same inputs:
   - Stable ordering (sorted prefixes / offer names / members — never "order of discovery").
   - No timestamps, random ids, machine names, or other non-reproducible tokens in emitted text.
   - Document the ordering rules in the generator Design regions.
2. **Materialize or snapshot.** Choose a concrete regenerate surface the CI can run (checked-in golden files, a `dev`/`ganda` regenerate command that writes files, or Verify/snapshot files the CI regenerates). Record the choice in Results. The important part is: regenerate → `git diff --exit-code` (or equivalent) fails the job when anything changed.
3. **CI step.** Add a workflow / pipeline step that:
   - Builds / runs the regenerate path for the two families above.
   - Fails on any unstaged or unexpected diff (`git diff --exit-code`, possibly scoped to the generated paths).
4. **Local doc.** One short note in the relevant AGENTS.md / generator docs: how to regenerate locally and what a CI failure means (re-run regenerate and commit, or fix the generator).
5. **Tests.** Unit coverage that the generators emit deterministic, ordered output for a fixed fixture set (repeat-run equality).

**Out of scope:** other analyzers/generators (page, FastEndpoints, TypedId, etc.); a 2× time check-in; Anatomy itself.

## Acceptance criteria

- Both scoped generators are deterministic on a fixed fixture (documented ordering; no timestamps).
- CI regenerates their output and fails on any diff.
- A deliberate one-character change to a golden/generated artifact (or skipping regenerate after a contract change) fails CI; regenerating and committing clears it.
- Docs say how to regenerate locally.
- Build is warning-free; tests green.

## Checklist

- [x] Audit current offer + YARP generators for non-determinism (ordering, timestamps)
- [x] Fix determinism; document ordering in Design regions
- [x] Choose regenerate surface (golden files / regenerate command / snapshots) and record why
- [x] Add CI step: regenerate + `git diff --exit-code` (scoped)
- [x] Local regenerate docs
- [x] Determinism unit tests on fixtures
- [ ] One PR; merge via `ganda pr merge`

## Session

- Created: 785761 (2026-10-11)
- Implement: ingress golden + fast-job diff. Offer-record generator stays removed (task 282).
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17T15:12:43Z
- Review oracle: tw-implementation-review, effort 2, roster general (claude-opus-5-5, session not exposed in print mode). 1 round. Disposition accepted-exceptions — 2026-10-17
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-17T15:16:38Z

## Review

- Round 1: Done

## Notes

Source: ideas borrowed from github.com/wheresryan22/anatomy, approved by the Skeptic review on 2026-10-12.

Skeptic verdict (2026-10-12): GREEN for this item. Cheap, standard, catches real drift. Scope to server offers and YARP routes; make generators deterministic first.

Related done work: task 107 (YARP ingress route list from web-contracts), tasks 280–282 (server-offer compile-time agreement + Offerable generation + availability flags).

Filing only. Do not claim-for-work or launch until Steven asks.

## Goal (implementer restatement)

Generated ingress prefixes and the retired server-offer emit must not drift from their source of truth. I will make the live generator byte-stable, check in the output it writes, and fail CI when a regenerate does not match that check-in.

Acceptance I am holding this change to:

- `IngressRoutePrefixGenerator` emits the same text on every run of a fixed fixture: prefixes sorted ordinally, case-only duplicates collapsed to the ordinal-least spelling, no timestamps or machine tokens. The ordering rule is in its Design region.
- The `[Offerable]` offer-record generator from tasks 280–281 is gone (task 282 deleted it and left typed availability flags, which are hand-written, not generated). There is no offer file to regenerate. I will not invent a second generator. The audit and the regenerate command record that, and the command still has a hook so a future offer emit joins the same diff.
- CI builds the two ingress hosts, writes `goldens/ingress/WebServerApiRoutePrefixes.g.cs`, and `git diff --exit-code` fails if that file changed. A one-character edit of the golden, or a contract change without regenerate, fails the job. Regenerating and committing clears it.
- `AGENTS.md` says how to regenerate locally and what the CI failure means.
- The generator has a repeat-run equality test. Build stays warning-free and the touched tests pass.

## Results

The live family is the YARP ingress prefix list. Task 282 deleted the `[Offerable]` generator, its attributes, and TWE012–014 / TWA0029–0031. Availability flags (`CanRevoke`, `CanRename`, `CanLinkMicrosoft365`) are hand-written on the contracts. There is no offer file to regenerate, and this change does not bring that generator back. `dev regenerate-generated` prints that there is no server-offer golden.

Ingress was not byte-stable. Prefixes followed reference order, and a case-only pair (`api/Roles` vs `api/roles`) kept whichever spelling was seen first. The generator now walks marked assemblies by name (ordinal), sorts routes by template then assembly name, collapses case-only spellings to the ordinal-least one, and emits the distinct prefixes sorted ordinal. Diagnostics use the same sorts. The file is UTF-8 with no BOM and ends with one LF. Those rules are in the generator Design region.

Regenerate surface: the checked-in golden `goldens/ingress/WebServerApiRoutePrefixes.g.cs`, written by `dev regenerate-generated`. `artifacts/` is gitignored, so the compiler copy cannot be the diff subject. The golden is not a Compile item, because compiling it would duplicate `WebServerApiRoutePrefixes`. The command builds yarp and the Aspire AppHost in Release and writes the golden only when both emits match after LF and BOM normalization. A disagreement leaves the golden untouched and exits 1.

CI: the `fast` job, after the pipeline succeeds, runs `dotnet run tools/dev-cli/dev.cs -- regenerate-generated`, then `git diff --exit-code -- goldens/ingress`, then `git status --porcelain -- goldens/ingress` so an untracked golden fails too. Push and pull_request path filters include `goldens/**`, and detect-paths includes `goldens/`.

`AGENTS.md` ("Generated ingress prefixes") says how to regenerate locally and what a CI failure means.

This branch is stacked on `feature/agent-surface-hardening` (merge of #533). It is not based on `master`.

### How to validate

Smoke:

```bash
dotnet run tools/dev-cli/dev.cs -- regenerate-generated
git diff --exit-code -- goldens/ingress
git status --porcelain -- goldens/ingress
```

From `tests/analyzers/timewarp-architecture-sourcegenerator-tests`:

```bash
dotnet test -c Release -- --filter-class IngressRoutePrefixGenerator_
```

From `tests/tools/dev-cli-tests`:

```bash
dotnet test -c Release -- --filter-class GeneratedIngressPrefixes_
```

Expect:

- The command prints `Wrote goldens/ingress/WebServerApiRoutePrefixes.g.cs` and `Server-offer golden: none. Task 282 removed the Offerable generator; availability flags are hand-written.` Both host builds report 0 warnings and 0 errors. yarp and the Aspire AppHost emit the same 914-byte file (no BOM, trailing LF), and that file is the golden.
- When the golden is already committed and matches, `git diff --exit-code -- goldens/ingress` exits 0. A second regenerate does not dirty it. Confirmed on this worktree after the second run.
- Changing one character of the staged golden (`api/tip` to `api/tiP`) makes `git diff --exit-code -- goldens/ingress` exit 1. Running the command again restores `api/tip` and a clean unstaged diff. Confirmed on this worktree.
- `IngressRoutePrefixGenerator_` : 15 passed, including repeat-run exact source equality and case-spelling collapse that does not depend on reference order.
- `GeneratedIngressPrefixes_` : 4 passed (LF and BOM normalization, the two host projects, agreeing copies succeed, disagreeing copies fail).

### Review disposition

- Rounds: 1. Effort 2, roster general (review oracle).
- Final counts: bug 0, suggestion 0, nit 1. Open 0, fixed 0, wontfix 1.
- Disposition: **accepted-exceptions**. M1 (nit) is wontfix: the CI drift failure prints a raw diff with no remedy echo, but the remedy is in the step comment, AGENTS.md, and the command Design region.
- The reviewer re-ran and confirmed: generator tests 15/15, dev-cli tests 4/4, `ganda repo audit` clean, and the golden byte-identical to both host emits.
- Restatement vs task: it matches, so no misread.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
