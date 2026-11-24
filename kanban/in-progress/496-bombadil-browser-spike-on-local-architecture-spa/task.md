# Bombadil browser spike on local Architecture SPA

## Description

Research only, time-boxed. Pin the v0.7.10 tagged Linux binary of [antithesishq/bombadil](https://github.com/antithesishq/bombadil) (not `main`). Run one 15-minute `browser test` with default properties only against a local Architecture SPA run, `inspect` the output, and report real bugs vs false positives (dev-mode 404s, auth redirects) plus setup time (Chromium on twe-001 unverified). Try `shrink` only if a violation appears.

Source: [Bombadil eval](https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-16-bombadil.md) ([timewarp-intel PR #130](https://github.com/TimeWarpEngineering/timewarp-intel/pull/130)). Busara verdict: option A GREEN; B/C/D/E/G/I KILL; F/H/J folded in.

Filed 2026-11-17 at Steven's request (via Amina, 8:00 PM BKK).

## Requirements

1. **Pin the v0.7.10 tagged Linux binary.** Use the tagged release binary from https://github.com/antithesishq/bombadil, not `main`. Record the exact version and binary source in Results.
2. **Local Architecture SPA.** Start a local run of the Architecture SPA and point the test at that instance.
3. **One 15-minute `browser test`.** Run a single 15-minute `browser test` with default properties only. No custom TypeScript spec.
4. **`inspect` the output.** Triage findings into real bugs vs false positives, especially dev-mode 404s and auth redirects.
5. **Setup time.** Record how long setup took, including whether Chromium is available on twe-001 (currently unverified).
6. **`shrink` only if a violation appears.** Do not run shrink unless the test reports a violation.
7. **Report, do not add a CI job.** The deliverable is the triage report in Results. Do not add a CI job in this task.

## Goal (implementer restatement)

Find out, in one time-boxed run, whether Bombadil's default properties find real bugs in the
Architecture SPA or mostly noise. Done means:

- The Linux binary from the v0.7.10 GitHub release is used, with its version and sha256 recorded.
- One headless `browser test` runs for 15 minutes with no spec file against a local SPA run.
- Each property violation in the trace is triaged as a real bug or a false positive (dev-mode
  404, auth redirect, tool noise), with evidence.
- Setup time and how Chromium was found on twe-001 are recorded.
- `shrink` runs only if there is a violation.
- No product code, no TypeScript spec, and no CI job are added. The report lives under Results.

## Out of scope

- A custom TypeScript spec
- A CI job

Busara gave these a KILL: B, C, D, E, G, I. F, H, and J were folded in. Do not expand this spike into those killed options.

## Checklist

- [x] Pin the v0.7.10 tagged Linux binary (not `main`) and record the version
- [x] Start a local Architecture SPA run (used the local master AppHost run that was already up; see Results)
- [x] Run one 15-minute `browser test` with default properties only
- [x] `inspect` the output and report real bugs vs false positives (dev-mode 404s, auth redirects)
- [x] Record setup time, including Chromium availability on twe-001
- [x] Try `shrink` only if a violation appears (no violation, so shrink was not run)
- [x] Write the report in Results — no custom TypeScript spec, no CI job

## Session

- Created: 3509003 (2026-11-17)
- 2026-11-25 implementer (claude, headless `ganda task work`): pinned v0.7.10, ran the 15-minute
  test against the local master AppHost run, triaged the trace, wrote Results. Did not start an
  AppHost from this worktree: every worktree shares the AppHost `UserSecretsId`, and a worker
  `dev run` has rewritten the shared postgres password before.

## Results

### Verdict

No real bugs and no false positives. With default properties, Bombadil reached only two
anonymous routes (`/` and `/Login`) in 15 minutes. That is because of a gap in Bombadil's
default click generator, not the app. A 15-minute default-properties run on this SPA proves very
little. To be useful, Bombadil would need a way to click FluentUI v5 buttons and an
authenticated session. Both are outside this spike.

### Binary

- `bombadil 0.7.10` (`--version`), asset `bombadil-x86_64-linux` from the GitHub release tag
  `v0.7.10` (published 2026-10-21), fetched with
  `gh release download v0.7.10 -R antithesishq/bombadil -p bombadil-x86_64-linux`.
- sha256 `6f2664ca7d4067017b4efb26db5c29d6dcfecdc78131f96989d336f12420cc1b` matches the
  release digest. This is not a `main` build.

### Target

- `https://localhost:27609/` is the YARP ingress of the local master-checkout AppHost (web-server,
  api-server, grpc-server, postgres) that was already running on twe-001. It was started
  2026-11-25 01:21 +07.
- Not started from this worktree. Every task worktree shares the AppHost `UserSecretsId`, and a
  headless worker's `dev run` has rewritten the shared postgres password before. Pointing at the
  running instance satisfies "local Architecture SPA run" without that risk.
- The run wrote no data. Bombadil never submitted a form. It only typed into the search palette.

### Setup time and Chromium on twe-001

- Hands-on setup took about 1 minute, from the start of the download (03:07:29) to the test
  launch (03:08:31). The binary download took 5 s. Reading the manual and finding the two
  blockers below took a few more minutes.
- No system Chromium. Bombadil looks for `$CHROME`, then `chromium`, `google-chrome`, and
  `/usr/bin/...`. `~/.local/bin/google-chrome` exists, but it is a stale wrapper that points at
  `ms-playwright/chromium-1234`, which is gone. The usable browser was Playwright's Chrome for
  Testing 159.0.8154.6 (`~/.cache/ms-playwright/chromium-1255`).
- The local ingress uses the self-signed `CN=localhost` dev certificate, so headless Chrome
  refused it (`ERR_CERT_AUTHORITY_INVALID`). Bombadil has no flag to ignore cert errors. Fix: set
  `CHROME` to a run-scoped wrapper script that adds `--ignore-certificate-errors`. The NSS trust
  store was not changed.

### Run

```
CHROME=<wrapper> bombadil browser test --headless --time-limit 15m \
  --output-path /tmp/bombadil-496/run/out https://localhost:27609/
```

- There was no spec file, so the default properties ran. Output: `Test finished after time limit!`.
  That is exit 0, and it means no violations.
- 7,056 states (3,161 distinct) and 7,056 screenshots, 325 MB of output. Actions: 5,760 clicks,
  591 back, 416 type-text, 141 key presses, 60 waits, 58 reloads, 15 forward, 14 scrolls.
- Every state returned HTTP 200 `text/html`. `consoleErrors` and `uncaughtExceptions` stayed
  empty in every state, and `violations` was empty on every trace line.
- `inspect`: `bombadil browser inspect --no-open --port 18073 <out>` served the Bombadil Inspect
  UI. The triage below comes from the same `trace.jsonl` it reads, aggregated with a script.
- `shrink` was not run because there was no violation.

### Triage

| Item | Class | Evidence |
|------|-------|----------|
| Property violations | none | `violations` empty on all 7,056 trace lines |
| Dev-mode 404s | none seen | every `responseStatus` was 200 |
| Auth redirects | none seen | only anonymous routes were reached, so no protected route was hit |
| Exploration stuck on `/` and `/Login` | tool gap, not an app bug | see below |
| Ghosted or doubled screenshots | not triaged | Bombadil's JPEGs show overlapping layouts. A plain headless Chrome render of the same pages is clean. Likely a capture artifact. |

Why coverage stopped at two routes:

- Bombadil's default click generator registered a `fluent-button` only when it had an explicit
  `role="button"` attribute. Those were the theme toggle and the nav expand/collapse buttons
  (2,226 of the clicks).
- FluentUI v5 buttons without that attribute set their role through ElementInternals, so they
  were never offered as actions. That covered Home "Sign in" (`HomeSignIn`), "Ask AI"
  (`AskAiButton`), and both `/Login` buttons (`ContinueWithPasskey`, `CreatePasskey`).
- On `/Login` the passkey buttons render and show in the screenshots. A hydrated DOM dump
  confirms them. Still, all 76 `/Login` states had zero clickable points, so Bombadil could only
  go back, reload, or wait.
- The "External" nav group expands to off-origin links, which fall outside the origin boundary.
  The only route Bombadil left `/` for was `/Login`, through the search palette's "Sign in" row.
- Every authenticated feature is behind a passkey sign-in that the default actions cannot do.
  The `--cookie` option or a custom spec could reach it, but both are out of scope.

Not filed as follow-ups: making FluentUI buttons clickable, or seeding auth, needs a custom
TypeScript spec. Busara killed that option for this spike.

### How to validate

- Smoke: with a local Architecture AppHost running, run
  `CHROME=<wrapper adding --ignore-certificate-errors> bombadil-x86_64-linux browser test --headless --time-limit 1m --output-path <dir> https://localhost:<ingress-port>/`.
  Then `python3 -c` over `<dir>/trace.jsonl`: count `violations` and the distinct `state.url`.
- Expect: exit 0, `Test finished after time limit!`, zero violations, and only `/` and `/Login`
  among the URLs. No `ContinueWithPasskey` or `CreatePasskey` fingerprint appears in any
  `clickablePoints` snapshot.

### Gates

- `./bin/dev build`: passed (exit 0, "Build completed successfully", 35.0 s). This change edits
  only this task file, so no product code changed.
- Test files: none added or edited, and no production code changed, so no test file covers this
  change.
- `ganda repo audit`: the first run failed only `kanban-landed` (task 458's live claim kitchen
  differed from origin-home; nothing in this task). Boy-scout fix: running
  `ganda repo audit --fix --checks kanban-landed` landed 458's kitchen onto origin-home, and the
  run then passed (exit 0). The one advisory warning left is `vscode-window-icon`
  (`.vscode/settings.json` missing). It is non-blocking.

## Notes

- Tool: https://github.com/antithesishq/bombadil (pin v0.7.10 Linux binary)
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-16-bombadil.md
- Intel PR: https://github.com/TimeWarpEngineering/timewarp-intel/pull/130
- Busara verdict: A GREEN; B/C/D/E/G/I KILL; F/H/J folded in
