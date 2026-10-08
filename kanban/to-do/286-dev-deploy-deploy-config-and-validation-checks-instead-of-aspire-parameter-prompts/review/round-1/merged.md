# Round 1 — merged findings
**Date:** 2026-10-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 5 | 0 |

## Issues

M1–M10 map 1:1 to Issue 1–10 in `general.md` (single reviewer; no duplicates). All were fixed in 20de37319 and re-verified fixed in round 2.

| ID | Severity | Status | Summary | Disposition notes |
|----|----------|--------|---------|-------------------|
| M1 | suggestion | fixed | Preflight probes had no timeout | 20s `ProbeTimeout` via Amuru `WithTimeout` (Amuru 2.0.0-beta.2); a timed-out probe gets its own refusal |
| M2 | suggestion | fixed | A failed `user-secrets list` was reported as "all parameters missing" | `UserSecretsUnreadableMessage` is reported ahead of the missing list |
| M3 | suggestion | fixed | The required-parameter list could drift from the AppHost | Test scans program.cs + constants.cs |
| M4 | suggestion | fixed | Problem-combination rules were untested | Pure `CollectClusterProblems` / `CollectKubernetesProblems` + tests |
| M5 | suggestion | fixed | Deprovision forwarded no parameters | Resolves best-effort and forwards the set ones; never refuses |
| M6 | nit | fixed | Registry probe swallowed Ctrl+C and gave a vague reason | `when (!ct.IsCancellationRequested)`; the reason (e.g. TLS) is in the refusal |
| M7 | nit | fixed | Env var lookup was case-sensitive | Case-insensitive scan; an exact-case match wins |
| M8 | nit | fixed | Registry refusal printed a placeholder csproj path | Prints the real path |
| M9 | nit | fixed | Forwarded values could leak a secret | Design region and skill: forwarded parameters must be non-secret |
| M10 | nit | fixed | Design synopsis omitted `--Parameters` | Synopsis updated |

## Duplicates / conflicts

- None (single reviewer).
