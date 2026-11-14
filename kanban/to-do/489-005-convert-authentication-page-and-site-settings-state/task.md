# Convert AuthenticationPage and SiteSettingsState

## Description

Hardest conversion; do last.

- `SiteSettingsState` holds `GetSiteSettings.Response` whole instead of ~15 flattened fields.
- Remove `DraftAcceptedTenants` from the store. Tenant add/remove edit
  `Command.EntraAcceptedTenants` on the working copy; tenant lookup/resolution stays an action
  whose result the page applies to its copy.
- `Version`, `AssistantName`, `XaiByokMode`, `Configuration*` are not on
  `ISiteSettingsDetails` — handle per the decision made in the generator task.
- Replace `SyncDraftFromState()` and the repeated `Command.EntraAcceptedTenants = [..]` lines.
- Consider adding Cancel now that reset is cheap.

## Depends on

- 489-004

## Checklist

- [ ] State holds whole Response; update all readers
- [ ] Tenant draft out of store
- [ ] Page conversion
- [ ] Tests

## Results

## Parent

- 489
