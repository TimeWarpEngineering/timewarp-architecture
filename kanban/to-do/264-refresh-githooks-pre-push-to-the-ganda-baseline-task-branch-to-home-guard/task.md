# Refresh githooks pre-push to the ganda baseline (task branch to home guard)

## Description

Refresh this repo's `.githooks/pre-push.cs` (and the `.githooks/pre-push` shim if the baseline
changed it) to the current ganda repo baseline.

Ganda task 323 (timewarp-ganda PR #197, merged 2026-10-01) added a guard to the baseline
pre-push hook. It refuses a push whose local ref is `refs/heads/task/*` and whose destination
is the home branch (`master` / `main`), and the message names the task branch. Raw-sha pushes,
such as the `kanban publish` merge commit, stay allowed. Until each repo picks up the new hook,
`ganda repo audit` warns `memsearch-scaffold: .githooks/pre-push.cs (outdated)`, and the repo
lacks the guard.

## Requirements

1. Apply the baseline with ganda itself, not by hand:
   `ganda repo audit --fix --checks memsearch-scaffold`. Confirm that `.githooks/pre-push.cs`
   now matches the baseline and the warning is gone.
2. Keep any repo-specific hook content the baseline intends to preserve. If `--fix` would drop a
   local customization, stop and record it in Notes rather than overwrite it.
3. Re-run `ganda repo audit` and fix anything else it reports (boyscout welcome), then commit.
4. Smoke-test the hook without touching home:
   - Pipe a fake pre-push line into the hook,
     `refs/heads/task/x <sha> refs/heads/master <sha>`, and confirm it is refused.
   - Pipe `<sha> <sha> refs/heads/master <sha>` and confirm it is allowed.

   Do not push to master to test.

## Checklist

- [x] `.githooks/pre-push.cs` refreshed via `ganda repo audit --fix --checks memsearch-scaffold`
- [x] Audit clean (no `memsearch-scaffold` warning)
- [x] Hook smoke test: task→home refused, raw sha→home allowed (stdin simulation only)
- [x] Gates per this repo's `tw-pr` (a hook-only change needs no full build unless the skill's
      scope table says otherwise)
- [x] Implementation review (disposition clean); host `open-pr`

## Notes

- One of a set of identical tasks filed in each repo that carries `.githooks/pre-push.cs`:
  amuru, architecture, bayline, ganda, kiini, mediator, nuru, state, taratibu.
- Do not start any app host. Run builds serially and call `dotnet build-server shutdown` before
  finishing.

## Results

- `ganda repo audit --fix --checks memsearch-scaffold` refreshed `.githooks/pre-push.cs` to the
  ganda baseline (+19 lines, purely additive: the task/* → home guard). No repo-specific hook
  content was dropped; the `.githooks/pre-push` shim was unchanged by the baseline.
- `ganda repo audit` also reported `bin-dev` / `dev-cli-capabilities` errors: the fresh worktree
  had no `bin/dev` (gitignored AOT build). Fixed locally with
  `dotnet run tools/dev-cli/dev.cs -- self-install`; no committed change needed.
- Final audit: Passed 31 | Failed 0, exit 0.
- Hook-only change: no `dev build` / version bump required (no package or template code touched).

### Review disposition

- Rounds: 1; effort 1; roster: general.
- Final counts: bug 0 / suggestion 0 / nit 0 (0 open, 0 fixed, 0 wontfix).
- Disposition: **clean** — no findings; reviewer re-ran audit (all pass) and stdin smoke.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke (from the worktree root; stdin simulation only — never pushes):

```bash
ganda repo audit
S=$(git rev-parse HEAD); Z=0000000000000000000000000000000000000000
echo "refs/heads/task/x $S refs/heads/master $Z" | .githooks/pre-push origin x; echo "exit=$?"
echo "$S $S refs/heads/master $Z" | .githooks/pre-push origin x; echo "exit=$?"
```

Expect:

- Audit: `Passed: 31 | Failed: 0`, no `memsearch-scaffold` warning.
- task→home: stderr `Refusing push of task branch to home: task/x -> master.` (+ guidance), `exit=1`.
- raw sha→home (HEAD on a task branch): no output, `exit=0`.

## Session

- Created: 2026-10-01
- 2026-10-01: implemented via ganda task work (baseline applied, audit clean, smoke recorded).
- 2026-10-01: review oracle (claude, effort 1, general) — disposition clean.
