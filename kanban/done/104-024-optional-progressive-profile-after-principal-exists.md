# Optional progressive profile after principal exists

## Parent

104

## Description

Update display name/email/etc. after principal exists. Never required for passkey or agent register. Not on critical path.

## Requirements

- Optional fields
- Contract/endpoint style of template
- No gate on session/token

## Checklist

- [ ] Model fields — lives on **205**, not this id
- [ ] Update API — lives on **205**, not this id
- [ ] Tests — lives on **205**, not this id
- [x] Board close: superseded by **205**; `done` (not archived) so parent **104** can close

## Notes

Optional Wave 5.

**2026-08-26:** Pulled off epic **104**. Work continues as independent to-do **205**
(progressive profile + agent-human handoff after more domain exists). Archived so
104 no longer owns this higher-level product.

### Depends on

104-002

## Session

- Created: 2026-07-16
- 2026-08-26: pulled to **205**; archived so 104 no longer owns the product
- 2026-08-26: marked **done** (superseded stub) — parent-done treats archived as open (ganda 187)

## Results

Not implemented on this id. Progressive profile was pulled off epic **104** onto independent to-do **205**. This kitchen is a superseded stub: closed `done` so parent **104** can close (ganda parent-done: `Column != Done` includes archived). Product work stays on **205**. Do not revive this id.

### How to validate

**Smoke**

```bash
ganda kanban path 104-024
# Expect: …/kanban/done/104-024-optional-progressive-profile-after-principal-exists.md

ganda kanban path 205
# Expect: …/kanban/to-do/205-progressive-profile-and-agent-human-handoff-after-more-domain-exists.md
```

**Expect**

- 104-024 is under `kanban/done/` (not archived, not in-progress).
- **205** remains to-do. Profile model/API/tests are not on this id.
