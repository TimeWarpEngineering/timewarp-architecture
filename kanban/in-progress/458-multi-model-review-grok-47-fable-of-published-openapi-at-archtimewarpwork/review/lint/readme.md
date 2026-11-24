# Linter output — snapshot `openapi-v1.snapshot.json`

Run 2026-11-24 over a byte copy of the snapshot (`openapi.json` in the output is that copy).

| Tool | Version | Command | Result |
|------|---------|---------|--------|
| Redocly CLI | 2.83.0 | `npx -y @redocly/cli@latest lint openapi.json --format stylish` (built-in `recommended`) | exit 1: 97 errors, 24 warnings → `redocly.txt` |
| Spectral CLI | 6.17.1 | `npx -y @stoplight/spectral-cli@latest lint openapi.json -r .spectral.yaml -f text` with `extends: ["spectral:oas"]` | exit 0: 133 warnings, 0 errors → `spectral.txt` |

## Grouped

Redocly:

| Count | Level | Rule |
|------:|-------|------|
| 58 | error | `security-defined` — operation has no `security` and there is no root `security` |
| 38 | error | `operation-summary` — operation has no `summary` |
| 1 | error | `no-server-trailing-slash` — `https://arch.timewarp.work/` |
| 1 | warning | `operation-4xx-response` |
| 1 | warning | `no-ambiguous-paths` |
| 1 | warning | `info-license` |
| (rest) | warning | see file |

Spectral (`spectral:oas`):

| Count | Rule |
|------:|------|
| 79 | `operation-tag-defined` — tags used on operations are not declared in root `tags` |
| 35 | `operation-description` |
| 17 | `oas3-operation-security-defined` — scope (e.g. `feedback:read`) not listed among the scheme's scopes |
| 1 | `oas3-server-trailing-slash` |
| 1 | `info-contact` |

Neither tool reported a structural schema error (`struct` / `oas3-schema`) against 3.2.0. Spectral
6.17.1 does not formally know 3.2.0; it validated with its 3.x rules.
