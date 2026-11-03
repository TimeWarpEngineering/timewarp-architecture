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

## Goal (implementer restatement)

Bring the six over-limit non-test and co-located-runfile files under source/container-apps/web to
500 lines or fewer without changing behavior.

Acceptance criteria:

- `XaiChatUpstream` is decomposed by responsibility: the per-key chat client cache, the
  deterministic fake client, and the relay-to-Microsoft.Extensions.AI mapping each become their
  own named type in their own kebab file, with Purpose and Design regions. One top-level type per
  file. No `partial` to get under the limit.
- `EntraTicketProcessor` keeps its public surface (ProcessAsync, CompleteBootstrapCreateAsync,
  AttachEntraToPrincipalAsync, ModeLink/ModeBootstrap, DI registration unchanged) and hands the
  link/attach path and the sign-in stamp to extracted types, each with its own Design notes.
- The four co-located Jaribu runfiles are split by scenario into several runfiles, each 500 lines
  or fewer, each still runnable alone with `dotnet run <file>.cs` and still compiled by the
  `web-jaribu-tests` JARIBU_MULTI aggregator with no duplicate type names. Shared test helpers
  are written once (no copy per file). C-create / C-share lane rules are kept.
- `dev build` is 0 warnings, the web-server integration tests and the web-jaribu-tests
  aggregator pass, and `ganda repo audit` is clean.

## Checklist

- [x] Decompose xai-chat-upstream-server by responsibility
- [x] Decompose entra-ticket-processor-application
- [x] Split the four co-located runfiles by scenario
- [x] dev build and tests green

## Results

All six files are under 500 lines, and the two EF migration files stay exempt. No class uses
`partial` to get under the limit, and each new file has one top-level type, Purpose and Design
regions, and a kebab name.

**Product classes** (`source/container-apps/web/features/`)

- `agent-chat/xai-chat-upstream-server.cs` went from 773 to 282 lines. `XaiChatUpstream` keeps
  key resolution, the per-turn key slot, and the completion flow. Three types moved out:
  - `XaiChatClientCache` (`xai-chat-client-cache-server.cs`, 181 lines) holds the bounded
    per-key client cache, use counting, and deferred disposal.
    `XaiChatUpstream.MaxCachedClients` now forwards to the cache's constant, so the tests did
    not change.
  - `XaiFakeChatClient` (`xai-fake-chat-client-server.cs`, 236 lines) is the scripted fake. It
    was a nested class before.
  - `XaiChatMapping` (`xai-chat-mapping-server.cs`, 148 lines) is the relay-to-M.E.AI mapping.
    It was the second top-level type in the old file.
- `identity/entra-ticket-processor-application.cs` went from 507 to 282 lines.
  `EntraTicketProcessor` keeps its public surface, its constructor, and its DI registration. It
  does the issuer check, the mode dispatch, sync-hit and bootstrap, and the bootstrap mint. Two
  internal types moved out, and the processor builds both from its injected stores:
  - `EntraAccountLinker` (`entra-account-linker-application.cs`, 234 lines) handles link,
    link-merge, the already-have attach, and the rule of one Microsoft 365 account per principal.
  - `EntraSignInStamp` (`entra-sign-in-stamp-application.cs`, 67 lines) does the last-used stamp
    and the account hint refresh.

  The old Design region is now split across the three files, each with the notes for its own
  part.

**Co-located runfiles**

Each runfile was split by scenario. Helpers that several runfiles share are written once, in a
support file that each runfile pulls in with `#:include`. The support file name ends in
`-tests.cs`, so the aggregator compiles it once. This is a new convention, so I documented it in
`skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`.

- Feedback triage. Each runfile's test count is the same standalone as before (42 tests):
  - `feedback-triage-tests.cs` (335 lines) has item triage and the authorization surface.
  - `feedback-admin-tests.cs` (440) has the admin list, sort, paging, detail, status, replies,
    and notes.
  - `feedback-owner-name-tests.cs` (224) has the owner names.
  - The support file is `feedback-triage-world-tests.cs` (265).
- Feedback attachment (22 tests):
  - `feedback-attachment-tests.cs` (296) has limits, read, upload, download, headers, and remove.
  - `feedback-attachment-submit-tests.cs` (184) has link, submit, and the race.
  - The support file is `feedback-attachment-world-tests.cs` (349). Its helpers are renamed
    `Attachment*` so they do not collide in the aggregator.
- Permission evaluator (19 tests):
  - `permission-evaluator-tests.cs` (387) has the evaluator and `InMemoryRolePermissionStore`.
  - `permission-evaluator-agent-tests.cs` (183) has the agent-token tests.
  - The support file is `permission-evaluator-fakes-tests.cs` (63).
- User xAI key (12 tests):
  - `user-xai-api-key-tests.cs` (287).
  - The support file is `user-xai-api-key-harness-tests.cs` (240). Its helpers are renamed
    `XaiKeyTest*`.
  - Boy scout: the old file failed to build standalone with CA1859, and the aggregator never
    caught it. Two harness helpers now return `Task<ProtectedSecretWrite>`.

Test logic, assertions, and test names did not change. Every split class keeps C-create: none of
these classes boots a host.

**Gates run**

- `dev build`: 0 warnings, 0 errors, and the client-name scan passed.
- `dotnet test` in `tests/container-apps/web/web-server-integration-tests`: 435 passed, 1
  skipped, 0 failed.
- `dotnet test` in `tests/container-apps/web/web-jaribu-tests`: 458 of 458 passed, the same as
  the baseline before the split.
- Standalone `dotnet run` of each new runfile passed, and the counts add up to the originals.
- `dev lint-skills` passed.
- `dev template-smoke` (default variant) passed. The generated web aggregator ran 458 of 458.
- `ganda repo audit`: exit 0. The only remaining item is the `kanban-landed` advisory for nine
  other tasks' claims that have no worktree. `ganda repo audit --fix` set the executable bit on
  the new split runfiles, and that is committed. Support files stay non-executable.

### How to validate

Smoke:

```bash
./bin/dev build
(cd tests/container-apps/web/web-jaribu-tests && dotnet test)
(cd tests/container-apps/web/web-server-integration-tests && dotnet test)
dotnet run source/container-apps/web/features/feedback/feedback-admin-tests.cs
git ls-files 'source/container-apps/web/*.cs' | grep -v /migrations/ | xargs wc -l | awk '$1 > 500 && $2 != "total"'
```

Expect:

- The build has 0 warnings.
- The web-jaribu-tests aggregator reports 458 total, 458 succeeded.
- The web-server integration tests report 0 failed.
- The standalone feedback-admin runfile reports 19 of 19 passed.
- The last command prints nothing, because no non-migration web `.cs` file is over 500 lines.

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implement oracle: implementer-claude (Opus 5.5) 2026-11-04. A Sonnet helper did the runfile splits, and I re-ran the gates myself.
