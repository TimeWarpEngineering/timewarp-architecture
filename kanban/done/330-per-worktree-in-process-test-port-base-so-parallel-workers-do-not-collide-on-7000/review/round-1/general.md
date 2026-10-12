# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** full branch diff vs master

## Summary

Moves the in-proc test port base from fixed 7000 to a per-checkout FNV-1a derived base (20000 + n·1500, 13 slots), keeps `TIMEWARP_TEST_PORT_BASE` as an exact override, makes `dev test` resolve once and export it, and switches template-smoke from fixed 17000 to the generated app's derived base (bumped off the repo's slot). Slot math checked: (39999−1444−20000)/1500+1 = 13, max port 39444. Precedence, range validation, root discovery order (runfile source dir → BaseDirectory → cwd) and collision message match the spec; no port is moved mid-run. Low risk; two text-accuracy issues.

## Issues

### Issue 1 — Severity: nit
- File: tests/common/timewarp-testing/in-proc-test-ports.cs:14
- Description: Design region says the derived set is clear of the ephemeral range, naming only Windows 49152+. Slots 32000–38000 overlap Linux's default ephemeral range 32768–60999, so the claim is wrong on the primary dev platform.
- Suggestion: State the overlap and why it is accepted (13 slots vs 8; collision fails loudly).
- Status: open

### Issue 2 — Severity: nit
- File: tests/common/timewarp-testing/in-proc-test-ports.cs:150
- Description: Override `Describe()` text says "`dev test` exports the derived base" even when the operator set the variable explicitly (then dev test exports the override, not the derived base) — misleading in collision errors.
- Suggestion: Reword to "checkout X would derive Y; `dev test` sets the variable for its children".
- Status: open
