# Feedback: an outside agent reviews feedback through the API

## Description

Steve wants more ways to act on feedback than changing its status: an agent reviews new
feedback, scores it, files cards on the board, and updates the item.

**Decision (Steve, 2026-10-28):** a process outside the app pulls feedback through the HTTP API,
authenticated as an agent, and acts on it. The template ships the API surface and documents how
to connect. It does not ship board integration, an email-to-Amina action, webhooks, or a server
MCP endpoint. The reviewer process (Amina, Akili, Busara, ganda cards) is Steve's own setup and
lives outside the template.

### What the template already has

- **Agent sign-in:** agent-key registration (`start-` / `complete-agent-key-registration`,
  `add-agent-key`) and token issuance (`start-` / `complete-agent-token-issuance`). Tokens are
  opaque bearer tokens on the `AgentTokenDefaults.Scheme` scheme. They last 15 minutes
  (`AgentTokenOptions.TokenLifetimeMinutes`, 1–60), with no refresh token; refreshing means
  running issuance again with the key.
- **Feedback scopes:** `feedback:read`, `feedback:note`, `feedback:severity` and
  `feedback:status` (`AgentScopes`), mapped to permissions in
  `agent-scope-permission-seed-application.cs`. "Let a scoped agent triage feedback without
  admin grants" (2026-10-17) already ships, with tests in `feedback-triage-tests.cs`.
- **Operations a reviewer needs:**
  - `list-feedback`, filtered by status, kind, severity, filed-from and title
    (`feedback-list-filter-application.cs`);
  - `get-feedback-for-admin`;
  - `download-feedback-attachment`;
  - `add-feedback-note`;
  - `set-feedback-severity`, which is the score;
  - `change-feedback-status`.
  
  `reply-to-feedback` talks to the person who filed it.

### What is missing

1. **A documented reviewer path.** Nothing tells a generated app's developer how to connect an
   outside reviewer. Write it in the skill that owns feedback, or in `tw-web-api-contracts`:
   - register an agent key;
   - grant the four feedback scopes;
   - issue a token, and reissue it before the 15 minutes run out;
   - list `Status = New`, then read, set severity, add a note with the summary and any card
     link, and move the status on.
   
   Moving the status off New is what stops the next pull from processing the item again.
2. **An end-to-end proof over HTTP.** An integration test plays the reviewer against the real
   endpoints: it registers a key, issues a token, lists New items, sets severity, adds a note and
   changes status. It also checks that the token is refused on anything outside its scopes. Use
   the existing in-proc host lanes, not mocks (`dont-mock-your-friends-testing`).
3. **Gaps the proof turns up.** For example, a list filter or an operation the reviewer needs
   that the agent scheme does not accept. Fix them in this repo. If a fix would belong in another
   repository, stop and report it in `## Results` instead; Steve decides.

### Known limit (record it in the docs, don't fix it here)

`IAgentTokenStore` is in memory on every host, even with `postgres` on. A restart or a second
instance invalidates live tokens. Short tokens make that cheap, because the reviewer just
reissues. A durable store is a separate task if a deployment scales out.

## Checklist

- [ ] Reviewer path documented: key, scopes, token reissue, the pull loop, and status as the processed marker
- [ ] Integration test runs the reviewer loop over HTTP with an agent token, including scope refusal
- [ ] Gaps the test exposes fixed in this repo (or reported, if they belong elsewhere)
- [ ] In-memory token store limit recorded in the docs
- [ ] `dev build` 0/0; affected suites pass; `dev lint-skills` when `skills/` changes

## Session

- Created: 1197730 (2026-10-12)
- Respecified: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28). Dropped developer claim, email
  to Amina, notifications, and a server MCP endpoint in favour of an agent pulling through the
  API.

## Notes

Related: done task 273 (feedback filings with id/permalink and optional email copy), to-do 327
(sortable admin feedback list).

The browser-side WebMCP surface (`WebMcpAgentSurface`) is a separate thing. A headless reviewer
does not use it.

## Learnings
