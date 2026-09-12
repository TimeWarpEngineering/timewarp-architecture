# Kanban Board

Task lifecycle is **`ganda kanban`**. Do not hand-number ids or invent filenames.

SSOT:

- `AGENTS.md` — **Task management**
- Skill **`tw-kanban`**

Columns are kebab folders under `kanban/`: `backlog/`, `to-do/`, `in-progress/`, `done/`,
`archived/`. Folder location **is** status. New work: `ganda kanban create "title"` (it assigns
the number). Then `move` / `done` to transition. Keep checklist / `## Session` current; commit
kanban mutations. See `kanban/task-template.md` for the body shape.

## Definition of Ready

Before moving a task from backlog to to-do, ensure it meets these criteria:

### API Feature Endpoint
- [ ] Data required by client has been defined
- [ ] Endpoint requirements are clear

### Client Feature
- [ ] Figma designs complete (if UI work)
- [ ] Requirements and acceptance criteria defined
- [ ] Dependencies identified and available

## Definition of Done

Tasks are considered complete when they meet the appropriate criteria:

### API Endpoint

**Implementation:**
- [ ] Server
  - [ ] *Endpoint (required)
  - [ ] Server side only Validator
  - [ ] Mapper
  - [ ] *Handler (required)
- [ ] Api
  - [ ] *Request (required)
  - [ ] *Response (required)
  - [ ] *RequestValidator (required)

**Integration Tests (Jaribu):**
- [ ] *Handler Tests (required)
  - [ ] *Returns a valid Response given a valid Request via Handler
- [ ] *Endpoint Tests (required)
  - [ ] *Returns valid http Response given valid http Request via Endpoint
  - [ ] *Should throw a validation error given invalid Request (only need to test one validation rule)
- [ ] *RequestValidator Tests (required - test all validation rules)

**Documentation:**
- [ ] *Request class and properties (required)
- [ ] *Response class and properties (required)

### Client Feature

**Implementation:**
- [ ] *State (required)
- [ ] Actions
- [ ] Pipeline
- [ ] Notification
- [ ] Components
- [ ] Pages

**Integration Tests:**
- [ ] State
  - [ ] ShouldClone
  - [ ] ShouldSerialize (To support Redux DevTools)
- [ ] Every Action should have at least a positive test

**End-to-end Tests:**
- [ ] Test each Page can at least render without error given valid states
- [ ] Test happy paths for each primary use case

*Items marked with `*` are required. Others are optional based on feature needs.*
