# Upstream Crunchit portal patterns to TimeWarp Architecture

## Description

The Crunchit portal (`Crunchitfs/crunchit`) built reusable web-spa UI patterns on top of this
template. This parent tracks bringing them into the template as generic components, so every
generated app gets them. Each child is one pattern. The generated code carries no client name.

Child specs are written before each child is launched. As of 2026-10-22, only 379-001 has a full
spec; the other children are still title-only.

## Checklist

- [ ] 379-001 PageHeader and SectionHeader
- [ ] 379-003 KpiCard and strip layout
- [ ] 379-004 FilterBar and FilterSelect
- [ ] 379-005 Tabs and SegmentedControl
- [ ] 379-006 Avatar initials circle
- [ ] 379-007 EmptyState, full and inline
- [ ] 379-008 Drawer slide-over shell
- [ ] 379-009 ConfirmDialog to replace confirm/alert
- [ ] 379-010 IntegrationCard and SyncBar
- [ ] 379-011 GroupedTable, TableGroup and CountChip
- [ ] 379-012 ChartCard frame and trend/SLA class pills
- [ ] 379-013 DevNote, RoleBadge, RoleCard and PermissionGrid
- [ ] 379-014 BrandOptions white-label theming
- [ ] 379-015 Shell slots (user card, pinned bottom nav, nav group, search)
- [ ] 379-016 SignInOptions (passkey, Microsoft 365 only, staff)
- [ ] 379-017 Extend the StyleGuidePage contract for the upstreamed parts

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- There is no 379-002.
- 379-001 decision (Steve 2026-10-22): the page header lives in `TimeWarpPage` slots, not in a
  separate PageHeader component. Later children that touch the shell (379-015) build on those
  slots.
