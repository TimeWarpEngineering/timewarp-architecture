# Fix FluentCheckbox Value bind on Principals and Roles Save

## Description

FluentUI v5 `FluentCheckbox` is `FluentInputBase<bool>` with `Value`/`ValueChanged`.
`Checked`/`CheckedChanged` are not parameters — they splat onto the web component, so the
box toggles visually while TimeWarp.State drafts never update. Save PUT the old roles.

## Checklist

- [x] PrincipalsPage bind Value/ValueChanged
- [x] RolesListPage bind Value/ValueChanged
- [x] Results + How to validate

## Results

Root cause: v5 checkbox API. Save and EF store were fine; draft never contained Developer.

### How to validate

**Smoke**

1. Fresh session, first Create (Administrator + Member).
2. Admin → Principals → check Developer → Save.
3. Expect: Developer stays checked; Developer nav appears after session refresh (or hard nav).
4. Leave page and return: Developer still checked.

**Expect:** PUT `.../principals/{id}/roles` body `roleIds` includes Developer Guid.

## Session

- 2026-08-13: diagnosed from live repro; FluentCheckbox v5 has no Checked parameter.
