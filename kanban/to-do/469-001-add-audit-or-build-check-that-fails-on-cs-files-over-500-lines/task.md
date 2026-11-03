# Add audit or build check that fails on .cs files over 500 lines

## Parent

469

## Description

Add a check that fails when a tracked `.cs` file exceeds 500 lines. Decide whether it lives in
`ganda repo audit` (repo-wide, runs in tw-pr gates) or as an analyzer or MSBuild target in this
repo (runs in `dev build`, ships to generated apps). Prefer the one that also protects generated
apps, since this repo is the template. Record the decision in the check's Design region.

## Requirements

- Threshold 500 lines, configurable in one place.
- Exempt generated code: EF migrations under `platform/postgres/migrations/`, `*.Designer.cs`,
  `*.g.cs`, `goldens/`, and `obj/`. Decide whether `kanban/**` research scripts are exempt.
- Clear message naming the file and its line count.
- Enabled only after 469-002 through 469-006 merge. Until then the check may land disabled or
  as a warning, but `dev build` must stay 0 warnings, so a disabled check is the safer landing.

## Depends on

- 469-002
- 469-003
- 469-004
- 469-005
- 469-006

## Checklist

- [ ] Choose audit vs analyzer and record why
- [ ] Implement with exemptions
- [ ] Tests for threshold, exemption, and message
- [ ] Enable once master has no offenders

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
