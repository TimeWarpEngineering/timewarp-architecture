# Roles UI permission membership editor with last-admin and protected-core lockout guards

**Parent:** 182 · **Order:** D (after 182-003) · **Depends on:** 182-001–003

## Description

Admin can edit which permissions a role includes. **Must not ship without lockout guards** (disposition blocking #5).

## Requirements

- Roles UI: multi-select / matrix from permission registry for role membership.
- Optional read-only Permissions catalog page gated `admin.*`.
- **Last-admin:** SetPrincipalRoles cannot remove the last principal who holds a role granting `admin.principals.manage` (409).
- **Protected-core:** Administrator (or last role granting core admin permissions) cannot have core `admin.*` stripped — prefer system-role rule.
- Tests for both lockouts (resource-check teaching exemplars).

## Checklist

- [ ] UI membership editor
- [ ] Last-admin guard + tests
- [ ] Protected-core guard + tests
- [ ] Results + How to validate
