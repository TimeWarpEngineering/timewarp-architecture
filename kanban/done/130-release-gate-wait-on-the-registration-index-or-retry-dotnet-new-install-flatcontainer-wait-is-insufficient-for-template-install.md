# Release gate: wait on the registration index (or retry dotnet new install) - flatcontainer wait is insufficient for template install

## Description

Live failure, first real release through the 126-003 post-publish gate (v2.0.0-beta.8,
2026-07-28, run 30332386426): the gate's availability-wait polls **flatcontainer** and
correctly saw all 11 packages live (attempt 9), then immediately ran
`dotnet new install TimeWarp.Architecture@2.0.0-beta.8` — which failed exit 103 "not found in
NuGet feeds", failing the release run, because **`dotnet new install` resolves through
nuget.org's search/registration endpoint, which lags flatcontainer**. Restore uses
flatcontainer; template INSTALL does not. This is precisely the propagation nuance the 126 RFC
ballot's adversarial reviewer flagged (flatcontainer lag is minutes and is the restore path;
other indexes lag longer).

The publish itself succeeded — the red release was a false negative from the gate, costing a
manual rerun. Block semantics (maintainer ruling on 126-003) make gate false-negatives
expensive; the wait must match what the gated step actually queries.

**Fix options (implementer picks; either is acceptable):**

- **A (lean):** wrap the `dotnet new install` step itself in the same bounded retry/backoff
  loop used for flatcontainer (install IS the availability probe for the index it uses — no
  second endpoint modeling needed, and it also covers any future step-specific index quirk).
- **B:** additionally poll the V3 search/registration resource for the template package id
  before attempting install (models the real dependency but adds an endpoint contract to
  maintain).

Keep the flatcontainer wait for the restore-path packages — it is correct for what restore
does. Add/extend the gate's log lines so each wait names the endpoint it is waiting on.

## Checklist

- [x] Locate the gate implementation (dev workflow release mode / template-publish-smoke in
      tools/dev-cli) and the flatcontainer wait loop
- [x] Implement A (retry the install with bounded backoff, distinct log line) — or B with
      recorded rationale
- [x] Regression evidence: cite run 30332386426 in the code comment (the failure this guards)
- [x] Gates: `dev build` 0/0; workflow-level change verified at next release (note in Results
      that live proof rides the next release run — same verification class as 126-003 itself)

## Notes

- Origin: v2.0.0-beta.8 release run failure analysis (2026-07-28). Packages were live on
  flatcontainer ~5 min post-push; registration lagged past the install attempt; a
  `--failed` rerun ~20 min later was expected to pass (see release record).
- Related: 126-003 (the gate), 124 notes ("flatcontainer had every version within minutes" —
  true, and insufficient for this step), 126 RFC D3 adversarial refinement.

## Session

- Created: 2026-07-28 — filed from live release-gate false negative.

## Results

**Landed** (commit `7bd9700e`, single file `tools/dev-cli/endpoints/template-publish-smoke-command.cs`,
+66/−15): Option A per the lean — the `dotnet new install` step now retries inside a bounded
budget (`InstallRetryBudget` = 15 min; 5s→60s doubling backoff, mirroring the flatcontainer
loop's shape) with log lines that name the endpoint being waited on ("template install /
registration index" vs "flatcontainer availability"). Flatcontainer wait untouched (correct for
the restore path). Regression citation (run 30332386426) in both the class Design region and
the budget constant's comment. Timeout preserves the prior failure contract (exit code,
combined-output dump).

**Verification:** `dev build` 0/0 via BOTH the AOT binary and the runfile path (dev-cli source
changed — stale-binary footgun applied proactively), `template-publish-smoke --help` compiles
and prints standalone; no dev-cli test coverage exists (verified, not fabricated —
`tests/tools/` has only agent-identity-cli-tests). `dev self-install` run afterward so
`./bin/dev` is fresh. **Live proof rides the next release run** — same verification class as
126-003 itself; the retry's first real exercise will be the next `gh release create`.
Review: orchestrator-inline diff verification (claims spot-checked against the committed hunks),
proportionate to a single-file bounded change with an already-adjudicated design.

## Session

- Executed: 2026-07-28 — implemented by Claude Sonnet subagent, verified/closed by orchestrator
  (Claude Fable). Filed same day as the live false negative it fixes.
