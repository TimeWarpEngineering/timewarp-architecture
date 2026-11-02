# Feedback: an outside agent reviews feedback through the API

## Description

Steve wants more ways to act on feedback than changing its status: an agent reviews new
feedback, scores it, files cards on the board, and updates the item.

**Decision (Steve, 2026-10-28):** a process outside the app pulls feedback through the HTTP API,
authenticated as an agent, and acts on it. The template ships the API surface and documents how
to connect. It does not ship board integration, an email-to-Amina action, webhooks, or a server
MCP endpoint. The reviewer process (Amina, Akili, Busara, ganda cards) is Steve's own setup and
lives outside the template.

**Decision (Steve, 2026-11-02):** the generated OpenAPI document is the reviewer guide. An
outside agent given only `/openapi/{doc}.json` must be able to run the whole review loop. The
document is generated from the code, so it cannot drift from it. Do not write a walkthrough
page in a skill.

### What the template already has

- **Agent sign-in:** agent-key registration (`start-` / `complete-agent-key-registration`,
  `add-agent-key`) and token issuance (`start-` / `complete-agent-token-issuance`). Tokens are
  opaque bearer tokens on the `AgentTokenDefaults.Scheme` scheme. They last 15 minutes
  (`AgentTokenOptions.TokenLifetimeMinutes`, 1–60), with no refresh token; refreshing means
  running issuance again with the key.
- **Feedback scopes:** `feedback:read`, `feedback:note`, `feedback:severity` and
  `feedback:status` (`AgentScopes`), mapped to permissions in
  `agent-scope-permission-seed-application.cs`.
- **Operations a reviewer needs:** `list-feedback` (filtered by status, kind, severity,
  filed-from and title; `feedback-list-filter-application.cs`), `get-feedback-for-admin`,
  `download-feedback-attachment`, `add-feedback-note`, `set-feedback-severity` (the score),
  `change-feedback-status`. `reply-to-feedback` stays on the human schemes.
- **Per-scope proof over HTTP (done task 378):** `Scoped_Given_Agent_Bearer_Tokens` and
  `Role_Assigned_Agent_Should_Not_Inherit_Human_Grants` in
  `tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-tests.cs`
  register real agent keys, issue tokens, and check that each feedback scope is allowed its own
  operation and refused the others, and that reply is 401 for an agent token. Do not rebuild
  these.
- **OpenAPI security per operation:** `open-api-bearer-security-server.cs` marks each
  agent-token operation with `JWTBearerAuth` and its one scope, and describes the 401 / 403
  responses (`invalid_token`, `insufficient_scope`). `open-api-bearer-security-tests.cs` checks
  every seed grant appears on its operation.

### What is missing

1. **OpenAPI carries what an agent cannot infer today.** Add it to the generated document, not
   to a skill:
   - **Getting a token.** The `JWTBearerAuth` scheme description names the steps in order:
     register a key (`start-` then `complete-agent-key-registration`), then issue a token
     (`start-` then `complete-agent-token-issuance`). The issuance operations' descriptions say
     what the client signs and what it sends back.
   - **Token lifetime.** The scheme description states the configured lifetime, read from
     `AgentTokenOptions.TokenLifetimeMinutes` at document build time (not a literal "15"), that
     there is no refresh token, and that the client reissues with its key before the token
     expires or after a 401 `invalid_token`.
   - **The processed marker.** The `list-feedback` operation description says a reviewer lists
     `Status = New`, and that moving an item's status off New is what keeps the next pull from
     processing it again. Put the text on the contract (summary / description) so it is
     generated, not hand-written in a transformer.
2. **A test proves the document is enough.** Two parts, in the web-server integration lane:
   - **Document test** (beside `open-api-bearer-security-tests.cs`): the scheme description
     names the four issuance operations and the configured lifetime; the `list-feedback`
     description names New and the processed marker.
   - **Reviewer loop test** (new method in `admin-feedback-authorization-tests.cs`, reusing
     `IssueTokenAsync`, `Bearer`, `PutSeverityAsync`, `PostNoteAsync`, `PutStatusAsync`): one
     token holding all four feedback scopes lists `Status = New` and finds the filed item, reads
     it with `get-feedback-for-admin`, sets severity, adds a note, and changes status. The next
     `Status = New` list does not contain the item. Use the existing in-proc host lane, not
     mocks (`dont-mock-your-friends-testing`). Do not wait out the token lifetime.
3. **Gaps the work turns up.** Task 378 already proved the agent scheme on every reviewer
   operation, so none are expected. If one appears (a filter or operation the agent scheme does
   not accept), fix it in this repo. If a fix would belong in another repository, stop and
   report it in `## Results` instead; Steve decides.

### Known limit (record it, don't fix it here)

`IAgentTokenStore` is in memory on every host, even with `postgres` on. A restart or a second
instance invalidates live tokens. Short tokens make that cheap, because the reviewer just
reissues. This is a deployment fact, not an API fact, so it goes in one line in
`skills/tw-deploy/SKILL.md`, beside the Data Protection "every replica" section, not in
OpenAPI. A durable store is a separate task if
a deployment scales out.

## Goal (implementer restatement)

An outside reviewer that is given only the generated OpenAPI document must be able to run the
feedback review loop. The document itself has to say how to register an agent key, how to issue
a bearer token, how long that token lasts, that there is no refresh token, and when to reissue.
`list-feedback` has to say that a reviewer lists `Status = New`, and that moving an item off New
is the processed marker so the next pull skips it. That text lives on the contracts and the
scheme description, generated from code, including the lifetime read from
`AgentTokenOptions.TokenLifetimeMinutes` at document build time.

Acceptance:

- The `JWTBearerAuth` scheme description names `start-agent-key-registration`,
  `complete-agent-key-registration`, `start-agent-token-issuance`, and
  `complete-agent-token-issuance` in that order, states the configured lifetime, says there is
  no refresh token, and says the client reissues with its key before expiry or after a 401
  `invalid_token`.
- The issuance operation descriptions say what the client signs and what it sends back.
- The `list-feedback` contract description names `Status = New` and status-off-New as the
  processed marker.
- A web-server integration document test asserts those strings, and a reviewer-loop test uses
  one four-scope agent token to list New, read, set severity, add a note, change status, and
  confirm the item is gone from the next New list. Existing per-scope tests stay as they are.
- The in-memory `IAgentTokenStore` limit is one line in `skills/tw-deploy/SKILL.md` beside the
  Data Protection every-replica note. No board integration, webhooks, email action, or server
  MCP endpoint.

## Checklist

- [x] `JWTBearerAuth` scheme description names the key-registration and token-issuance steps in order
- [x] Scheme description states the lifetime from `AgentTokenOptions`, no refresh token, and when to reissue
- [x] Issuance operation descriptions say what the client signs and returns
- [x] `list-feedback` contract description states `Status = New` and status-off-New as the processed marker
- [x] OpenAPI document test asserts the above
- [x] Reviewer loop test: one four-scope token runs list New → read → severity → note → status, and the item leaves the New list
- [x] Gaps found fixed in this repo (or reported, if they belong elsewhere)
- [x] In-memory token store limit: one line in `tw-deploy`
- [x] `dev build` 0/0; web-server-integration-tests pass; `dev lint-skills` (skills/ changes)

## Session

- Created: 1197730 (2026-10-12)
- Respecified: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28). Dropped developer claim, email
  to Amina, notifications, and a server MCP endpoint in favour of an agent pulling through the
  API.
- Respecified: 431c3e64-803d-4442-8ca1-a6a431770511 (2026-11-02). Steve chose OpenAPI as the
  reviewer guide instead of a skill page. Narrowed the test to the loop and the processed marker,
  since task 378 already proves per-scope refusal.
- 2026-11-02 implementer: contract remarks were absent from generated endpoints because csc does
  not attach XML documentation to metadata references. The host now passes the contract `.xml` as
  an AdditionalFile and the generator reads it. Dropped `Produces(200, "Success")`: that string is
  a media type, and the chain replaces FastEndpoints' OneOf 200 metadata. No agent-scheme gap.
  `ganda repo audit` exit 0; advisory `kanban-landed` names other claims with no worktree, left
  alone.

## Results

The generated OpenAPI document is enough for an outside agent to run the feedback review loop.
`JWTBearerAuth` names the four ceremony operations in order, the lifetime from
`AgentTokenOptions` at document build, that there is no refresh token, and when to reissue.
Issuance descriptions say what the client signs and sends back. `list-feedback` says a reviewer
lists `Status = New` and that moving status off New is the processed marker. One four-scope token
lists New, reads the item, sets severity, adds a note, changes status, and the next New list
omits the item. `IAgentTokenStore` stays in memory; `tw-deploy` records that a restart or a
second instance invalidates live tokens and the reviewer reissues.

Generated apps that set `UseAnalyzerPackages` keep the published generators package. They pick
up contract remarks when that package includes this generator. This repo's build uses the
project generator.

### How to validate

Smoke: `./bin/dev build`, then from `tests/container-apps/web/web-server-integration-tests` run
`dotnet test -c Release -- --filter-class OpenApiAgentReviewerGuide_` and
`dotnet test -c Release --no-build -- --filter-class AdminFeedbackAuthorization_`. Fetch
`/openapi/v1.json` from the web host and read `JWTBearerAuth`, the four ceremony operations, and
`GET /api/admin/feedback`.

Expect: build 0 warnings and 0 errors. `OpenApiAgentReviewerGuide_` 3 passed. The scheme
description names the four operations in order, the configured lifetime in minutes, no refresh
token, and reissue after `invalid_token`. Ceremony descriptions name the signing prefix, ECDSA
P-256, and the fields sent back. `list-feedback` names `Status = New`, `processed marker`, and
`off New`. `AdminFeedbackAuthorization_` passed, including the four-scope loop that removes the
filed item from the next New list. Full web-server-integration-tests: 430 passed, 1 skipped.
`./bin/dev lint-skills` passed.

## Notes

Related: done task 273 (feedback filings with id/permalink and optional email copy), done task
378 (scoped agent feedback triage), to-do 327 (sortable admin feedback list).

The browser-side WebMCP surface (`WebMcpAgentSurface`) is a separate thing. A headless reviewer
does not use it.

## Learnings
