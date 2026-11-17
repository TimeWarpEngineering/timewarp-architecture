# Bombadil browser spike on local Architecture SPA

## Description

Research only, size S, time-boxed. Pin the v0.7.10 tagged Linux binary of [antithesishq/bombadil](https://github.com/antithesishq/bombadil) (not `main`). Run one 15-minute `browser test` with default properties only against a local Architecture SPA run, `inspect` the output, and report real bugs vs false positives (dev-mode 404s, auth redirects) plus setup time (Chromium on twe-001 unverified). Try `shrink` only if a violation appears.

Source: [Bombadil eval](https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-16-bombadil.md) ([timewarp-intel PR #130](https://github.com/TimeWarpEngineering/timewarp-intel/pull/130)). Busara verdict: option A GREEN; B/C/D/E/G/I KILL; F/H/J folded in.

Filed 2026-11-17 at Steven's request (via Amina, 8:00 PM BKK). Not launched.

## Requirements

1. **Pin the v0.7.10 tagged Linux binary.** Use the tagged release binary from https://github.com/antithesishq/bombadil, not `main`. Record the exact version and binary source in Results.
2. **Local Architecture SPA.** Start a local run of the Architecture SPA and point the test at that instance.
3. **One 15-minute `browser test`.** Run a single 15-minute `browser test` with default properties only. No custom TypeScript spec.
4. **`inspect` the output.** Triage findings into real bugs vs false positives, especially dev-mode 404s and auth redirects.
5. **Setup time.** Record how long setup took, including whether Chromium is available on twe-001 (currently unverified).
6. **`shrink` only if a violation appears.** Do not run shrink unless the test reports a violation.
7. **Report, do not add a CI job.** The deliverable is the triage report in Results. Do not add a CI job in this task.

## Out of scope

- A custom TypeScript spec
- A CI job

Busara gave these a KILL: B, C, D, E, G, I. F, H, and J were folded in. Do not expand this spike into those killed options.

## Checklist

- [ ] Pin the v0.7.10 tagged Linux binary (not `main`) and record the version
- [ ] Start a local Architecture SPA run
- [ ] Run one 15-minute `browser test` with default properties only
- [ ] `inspect` the output and report real bugs vs false positives (dev-mode 404s, auth redirects)
- [ ] Record setup time, including Chromium availability on twe-001
- [ ] Try `shrink` only if a violation appears
- [ ] Write the report in Results — no custom TypeScript spec, no CI job

## Session

- Created: 3509003 (2026-11-17)

## Notes

- Tool: https://github.com/antithesishq/bombadil (pin v0.7.10 Linux binary)
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-16-bombadil.md
- Intel PR: https://github.com/TimeWarpEngineering/timewarp-intel/pull/130
- Busara verdict: A GREEN; B/C/D/E/G/I KILL; F/H/J folded in
- Filed only; not launched. Do not start until Steven says so.
