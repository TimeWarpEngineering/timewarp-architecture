# Task Template

> **IMPORTANT NOTE**: This template provides a set of optional suggestions and ideas to choose from when creating tasks. Not all sections or items are required for every task. When creating tasks, choose conservatively which elements should be included based on the specific needs of the task. As an AI assistant, I will select appropriate sections based on the task's complexity and requirements.

## Description

[Provide a brief description of the task, outlining its purpose and goals]

## Parent (optional)
<Reference to parent item like 001_user-registration>

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

> SSOT: `tw-agent-collaboration` (Results-before-done). Without this subsection, do not
> mark the task done.

**Smoke** — 1–5 copy-paste commands and/or UI steps:

```bash
# e.g. ./bin/dev run && curl -si …
```

**Expect** — concrete outcomes (status codes, headers, UI copy, pack id):

- …

**Automated gate** (when tests exist):

```bash
# e.g. cd tests/… && dotnet test -c Release -- --filter-class …
```

**Depends on** / **Not in scope** (when relevant): env flags, live chain, hardware, etc.
