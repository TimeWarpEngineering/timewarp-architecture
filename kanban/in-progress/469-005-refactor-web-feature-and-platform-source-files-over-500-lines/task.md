# Refactor web feature and platform source files over 500 lines

## Parent

469

## Description

Non-test source under source/container-apps/web that exceeds 500 lines. The two product
classes should be decomposed by responsibility. The co-located Jaribu runfiles are test code
that lives beside the feature (tw-feature-placement); split them by scenario into several
runfiles rather than reshaping the feature.

## Files

- source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs (773, 2 types)
- source/container-apps/web/features/identity/entra-ticket-processor-application.cs (507)
- source/container-apps/web/features/feedback/feedback-triage-tests.cs (1156, runfile)
- source/container-apps/web/features/feedback/feedback-attachment-tests.cs (777, runfile)
- source/container-apps/web/platform/authorization/permission-evaluator-tests.cs (577, runfile)
- source/container-apps/web/features/agent-chat/user-xai-api-key-tests.cs (511, runfile)

Exempt by the check, not refactored: PostgresDbContextModelSnapshot.cs and
20261102105501_AddAgentScopeGrants.Designer.cs under platform/postgres/migrations.

## Requirements

- Each file 500 lines or fewer. Slice isolation (TWA0009) respected for any new type.
- Behavior unchanged. Web-server integration tests and the co-located runfiles pass.
- Split runfiles keep the C-create / C-share lane conventions from
  skills/tw-feature-placement/references/co-located-jaribu-runfiles.md.

## Checklist

- [ ] Decompose xai-chat-upstream-server by responsibility
- [ ] Decompose entra-ticket-processor-application
- [ ] Split the four co-located runfiles by scenario
- [ ] dev build and tests green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
