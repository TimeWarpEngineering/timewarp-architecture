# Convert AiPage assistant-name and BYOK forms

## Description

`admin/ai/pages/AiPage.razor` forms at lines ~274 and ~400 copy flattened `SiteSettingsState`
fields with `SyncAssistantDraft` / `SyncByokDraft` plus `SyncUnedited*` dirty tracking via shadow
`Synced*` fields, and mutate a component field from the render block.

- Add small `I*Details` interfaces for `UpdateAssistantName.Command` and
  `UpdateXaiByokMode.Command` (or copy from the whole `GetSiteSettings.Response`).
- Delete the shadow dirty-tracking; dirty = working copy differs from stored Response.

## Depends on

- 489-001

## Checklist

- [ ] Interfaces / copy source
- [ ] Remove Sync* and Synced* code
- [ ] Tests

## Results

## Parent

- 489
