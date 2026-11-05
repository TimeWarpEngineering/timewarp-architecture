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

## Checklist

- [ ] Shared Roles projection; GetRole and GetRoles call it
- [ ] Shared attachment projection; upload handler and `FeedbackContractMap` call it
- [ ] Named arguments at every projection site in Roles and Feedback
- [ ] `dev build` 0 warnings, 0 errors; `dev test` green
- [ ] `ganda repo audit` exits 0

## Session

- Created: 1002142 (2026-11-05)

## Notes

Filed from task 476 (decision A, analyzer alone). Evidence: 476 `proposal.md` sections 2–4.
Related: 478 (agent-scope ordering in `Role`). Either order works; see the last requirement.
