# Share one projection for GetRole and GetRoles and for feedback attachments, with named arguments

## Description

Task 476 found duplicate hand-written projections that the contract-agreement analyzer
(TWA0039–TWA0043) cannot check, because it proves a projection *can* be written, not that the
handler wrote it correctly:

- GetRole (`get-role-handler-application.cs`) and GetRoles (`get-roles-handler-application.cs`)
  each project the same five Role members (`RoleId`, `Name`, `Description`, `Permissions`,
  `AgentScopes`), once to `GetRole.Response` and once to `GetRoles.RoleDto`.
- The feedback attachment projection is written twice: `FeedbackContractMap.AttachmentsAsync`
  (`feedback-contract-map-application.cs`) and the upload response in
  `upload-feedback-attachment/upload-feedback-attachment-handler-application.cs`.
- Positional arguments of the same type can be swapped and still compile
  (`new GetRole.Response(role.Id.Value, role.Description, role.Name, …)`), and optional constructor
  parameters (`GetRole.Response` `permissions`/`agentScopes`, `FeedbackAdminView` `severity`) can be
  dropped silently.

The rule is now in `tw-aggregate-pattern` ("Projection and mutation calls stay hand-written"): one
projection per aggregate and response shape, in one static method, with named arguments.

## Requirements

- One Roles projection method in the Roles application layer (for example `RoleContractMap`) used
  by both GetRole and GetRoles. If `RoleDto` and `GetRole.Response` stay separate types, the shared
  method projects the common shape once and each handler wraps it; do not keep two copies of the
  member list.
- One attachment projection used by both `FeedbackContractMap.AttachmentsAsync` and the upload
  handler.
- Every `new Response(…)`, `new RoleDto(…)`, `new FeedbackAdminView(…)`, and attachment constructor
  call in the Roles and Feedback application layers uses named arguments.
- Behavior is unchanged: existing Roles and Feedback tests pass without edits to their assertions.
- If task 478 lands first, the shared Roles projection reads `role.AgentScopes` as is. If not, keep
  the `AgentScopeOrder.OrderKnown` call inside the shared method, once.

## Goal (implementer restatement)

Stop writing the same projection twice. GetRole and GetRoles both build the five Role members
(`RoleId`, `Name`, `Description`, `Permissions`, `AgentScopes`); the upload handler and
`FeedbackContractMap.AttachmentsAsync` both build the five attachment members. Each pair gets one
static projection method in its slice's application layer that reads the domain once; the two
response types stay separate and each caller wraps the shared shape with named arguments.
Every response, DTO, admin-view, and attachment constructor call in the Roles and Feedback
application layers uses named arguments, so same-typed members cannot be swapped and a dropped
optional parameter shows in the diff.

Acceptance:

- One Roles projection (`RoleContractMap`), called by the GetRole and GetRoles handlers; it reads
  `role.AgentScopes` as is (task 478 landed, so no `AgentScopeOrder.OrderKnown` call).
- One attachment projection in `FeedbackContractMap`, called by `AttachmentsAsync` and the upload
  handler.
- Named arguments at every `new Response(…)`, `new RoleDto(…)`, `new Item(…)`,
  `new FeedbackAdminView(…)`, activity entry, and attachment constructor call in the Roles and
  Feedback application layers.
- No behavior change: Roles and Feedback tests pass with unchanged assertions.
- `dev build` is clean, `dev test` is green, and `ganda repo audit` exits 0.

## Checklist

- [x] Shared Roles projection; GetRole and GetRoles call it
- [x] Shared attachment projection; upload handler and `FeedbackContractMap` call it
- [x] Named arguments at every projection site in Roles and Feedback
- [x] `dev build` 0 warnings, 0 errors; `dev test` green
- [x] `ganda repo audit` exits 0

## Session

- Created: 1002142 (2026-11-05)
- 2026-11-05 implementer (claude-opus-5-5, ganda task work): added `RoleContractMap.Project` and
  `FeedbackContractMap.Project`, rewired GetRole, GetRoles, `AttachmentsAsync`, and the upload
  handler to them, and named the arguments at every response/DTO/admin-view/activity/attachment
  constructor call in the Roles and Feedback application layers. `dev build` clean, `dev test`
  23/23, `ganda repo audit` exit 0.

## Results

### What changed

- `source/container-apps/web/features/admin/roles/role-contract-map-application.cs` (new):
  `RoleContractMap.Project(Role)` reads `RoleId`, `Name`, `Description`, `Permissions`, and
  `AgentScopes` once into `RoleContractMap.RoleProjection`. Task 478 had landed, so it reads
  `role.AgentScopes` as is; there is no `AgentScopeOrder.OrderKnown` call.
- GetRole and GetRoles handlers call `RoleContractMap.Project` and wrap it into `GetRole.Response`
  / `GetRoles.RoleDto` with named arguments. The two contract types stay separate
  (`Response` implements `IRoleDetails` with settable details; `RoleDto` is an immutable row), so
  the wire shape is unchanged.
- `FeedbackContractMap.Project(FeedbackAttachment)` is the one attachment projection
  (`FeedbackContractMap.AttachmentProjection`). `AttachmentsAsync` wraps it into
  `GetFeedback.Attachment`; the upload handler wraps it into `UploadFeedbackAttachment.Response`.
  The upload handler no longer builds the download path itself.
- Named arguments at every response construction in Roles (create, get, list, set-permissions,
  set-agent-scopes) and Feedback (list, list-mine, get, get-for-admin, submit, change-status,
  set-severity, reply, add-note, reply-as-filer, upload, remove, download), plus
  `FeedbackAdminView` and `FeedbackActivityEntry` in `FeedbackContractMap`.
- Design regions in the touched handlers and both maps now point at the shared projection
  (task 479) and drop the "task 476 evaluates generating it" note.

Out of scope, not changed: `FeedbackListFilter` and `EmailMessage` constructors (not response
projections), the contract-side mock factories, and the Principals slice
(`set-principal-*`, `list-principals`, `get-principal`), which is not Roles.

### Gates

- `./bin/dev build`: Build succeeded, 0 warnings, 0 errors.
- `./bin/dev test --no-build -q`: 23 of 23 projects passed (including web-jaribu-tests,
  web-server-integration-tests, aspire-tests, web-spa-playwright-tests). No test files edited.
- `ganda repo audit`: exit 0, "Repository passes — 2 advisory (non-blocking) warning(s)". Both
  were already there: `kanban-landed` (other tasks' claims with no worktree on this machine) and
  `source-file-line-cap` (EF migration designers and an archived research runfile; that check is
  set to warning, and TWA0038 enforces the cap). Nothing to fix on this task.

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev test --no-build -q
grep -rn "RoleContractMap.Project\|FeedbackContractMap.Project\|Select(Project)" source/container-apps/web/features
```

Expect: build reports 0 warnings and 0 errors; `23 of 23 projects passed`; the grep shows
`RoleContractMap.Project` in the GetRole and GetRoles handlers and `FeedbackContractMap.Project`
in the upload handler and `AttachmentsAsync` (`Select(Project)`), with no other copy of either
member list. In the app, Admin > Roles list and role detail show the same permissions and agent
scopes as before, and a feedback attachment uploaded on the submit form shows the same name,
size, and download link before and after submit.

## Notes

Filed from task 476 (decision A, analyzer alone). Evidence: 476 `proposal.md` sections 2–4.
Related: 478 (agent-scope ordering in `Role`). Either order works; see the last requirement.
