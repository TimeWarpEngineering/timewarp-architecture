# Task Template

> **Note**: The sections below are suggestions. Not every section or item applies to every task.
> Include only what the task needs.

## Description

[Provide a brief description of the task, outlining its purpose and goals]

## Parent (optional)
<Parent task id>

## Requirements (optional)

[List any specific requirements or criteria that must be met for the task to be considered complete]

## Checklist (optional)
    Select relevant items from this checklist based on the task's needs. Not all items will apply to every task.

### Design
- [ ] Update Model
- [ ] Add/Update Tests
### Implementation
- [ ] Update Dependencies
- [ ] Update Relevant Configuration Settings
- [ ] Verify Functionality
### Documentation
- [ ] Update Documentation

## Notes (optional)

[Include any additional information, resources, or references relevant to the task]

## Implementation Notes (optional)

[Include notes while task is in progress]

## Results (required before done)

[What was implemented, files changed, key decisions, test outcomes]

### How to validate (required before done)

> Without this subsection, do not mark the task done.

**Smoke** — 1–5 copy-paste commands and/or UI steps:

```bash
# e.g. dotnet run --project source/container-apps/aspire/projects/aspire-app-host && curl -si …
```

**Expect** — concrete outcomes (status codes, headers, UI copy, pack id):

- …

**Automated gate** (when tests exist):

```bash
# e.g. cd tests/… && dotnet test -c Release -- --filter-class …
```

**Depends on** / **Not in scope** (when relevant): env flags, live chain, hardware, etc.
