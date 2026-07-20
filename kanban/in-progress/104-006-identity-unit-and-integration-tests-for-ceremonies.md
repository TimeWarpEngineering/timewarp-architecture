# Identity unit and integration tests for ceremonies

## Parent

104

## Description

Fixie + Shouldly coverage for passkey and agent key paths (mock WebAuthn where needed). Gate Wave 1.

## Requirements

- Unit tests for domain + crypto/challenge helpers
- Integration tests for register/login or register/token
- `dev test` green for new projects

## Checklist

- [ ] Unit suite
- [ ] Integration suite
- [ ] CI-safe

## Notes

Wave 1 exit criterion.

### Depends on

104-003, 104-004

## Session

- Created: 2026-07-16

### Implementation plan (104-006)

#### Verdict
Ceremony unit + integration coverage was front-loaded into 104-003/004/005. This task is the **Wave 1 gate**: evidence matrix, D5 Design closeout, re-verify green. Optional: quarantine 403 HTTP tests.

#### Do
1. Update Design regions that still say "D5 deferred to 104-006" — ceremony stores already use TimeProvider; entity wall-clock stamps remain OK for Wave 1
2. Run identity unit + web-contracts + web-server-integration (+ CLI) suites; record counts
3. Optional G3: quarantined principal 403 on passkey auth + agent token if store access is clean
4. Results with coverage matrix; mark done

#### Not in scope
Playwright (104-022), api-server bearer (104-030), new test projects, hardware WebAuthn

## Session
- Started: 2026-07-20 (tw-orchestrate-task 104-006)
- Plan: 2026-07-20
