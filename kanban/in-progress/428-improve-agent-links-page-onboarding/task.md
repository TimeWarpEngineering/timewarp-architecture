# Improve agent links page onboarding

## Goal (implementer restatement)

A signed-in person opening Agent links should understand what a link is, see their own account id, and copy a prompt that tells their agent how to request a link. The prompt is a bash recipe using openssl and curl: create a P-256 key, register it, mint a token with `identity:read`, and `POST api/agent-links` with the account id shown on the page. No packed client and no .NET SDK. An AOT client is skipped because a shell-capable agent already completes that ceremony. The page keeps the sentence "Paid service does not require a linked human." Browser coverage checks the new copy, and `dev build` plus `dev template-smoke` stay green.

Acceptance:

- The page explains that the agent proves its key, the person approves or denies, paid service does not need a link, and an approved link lets the agent fetch the `timewarp.humanUx/v1` handoff (`GetHumanUx`).
- A copy button copies a self-contained openssl + curl prompt that includes this person's account id and does not point at `tools/` or a dotnet client.
- The kitchen records the decision to skip an AOT client.
- "Your account id" shows `GetCurrentSession` principal id (`timewarp:principal_id`) with its own copy button.
- A Playwright test covers those page changes.
- `dev build` and `dev template-smoke` pass.

## Description

The AgentLinks page experiences a gap in the flow discovered while linking a live agent
(opencode session ses_e7d6c83cfffebh1UD5myX7NDeE, 2026-10-28):

- The agent-side ceremony works end to end (keygen → register → token → `POST api/agent-links`),
  but the page itself gives a human visitor zero orientation: just "Agents can request a link to you"
  and an empty list.
- A normal user has no visible way to learn their `HumanPrincipalId` (Profile shows no id; Settings
  shows only an opaque `PrincipalFingerprint` from task 253; the raw id is admin-only at
  `/Admin/Principals`, `<dd><code>@principal.PrincipalId.Value</code></dd>`), yet the link request —
  started by the agent (`RequestAgentHumanLink`, `POST api/agent-links` with `HumanPrincipalId`) —
  requires the human to supply that id out of band.
- `tools/agent-identity-cli/` is the only working reference client for the ceremony and it is NOT
  packed (`timewarp-architecture-template.csproj` pack allow-list: source/, tests/, msbuild/,
  .template.config/, skills/ — no tools/). External agents get no client and no docs beyond
  contract Design regions.

Scope of this task is the page onboarding; the identity/student prompts sections below are page
copy plus one shipped artifact decision. The deeper "human-initiated link code" flow (server resolves
a code instead of the raw id) is noted but out of scope here.

## Requirements

1. **Brief explanation of linking** — a short paragraph (or expandable section) on
   `AgentLinksPage.razor` explaining what it means to link an agent (agent proves possession of its
   key, requests a link to you; you approve or deny; paid service does not require a linked human;
   linked status lets the agent fetch the `timewarp.humanUx/v1` handoff document via `GetHumanUx`).
   Keep the existing `Paid service does not require a linked human.` sentence.
2. **Copy-paste prompt for the human's agent** — a copyable block the human can paste to their agent
   (Claude Code / opencode / whatever) that instructs the agent through the ceremony: register its
   P-256 key (`POST api/identity/agent/register/options` → sign
   `TimeWarp.Identity.AgentKey.Register.v1:` ‖ challenge → `POST api/identity/agent/register` →
   save KeyId), mint a token with `identity:read` (the `AgentLinkManageSelf` policy expands from it),
   then `POST api/agent-links` with the human's principal id shown on this page. The prompt must
   reference a shipped client or shipped skill, not `tools/` (see item 3).
3. **No shipped client required for agent targets** — proven live (2026-10-28, session
   ses_e7d6c83cfffebh1UD5myX7NDeE): a shell-capable agent completes the entire ceremony with only
   `openssl` + `curl` — keygen (`openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256`),
   register (sign `TimeWarp.Identity.AgentKey.Register.v1:` ‖ challenge with
   `openssl dgst -sha256 -sign`, DER = the server's required `Rfc3279DerSequence`), token issuance
   (same with the `Token.v1:` prefix, challenge delivered and voiced as a fresh token 200). The
   copy-paste prompt in requirement 2 is therefore a self-contained bash recipe; the CLI in
   `tools/agent-identity-cli/` stays maintainer-only and does NOT pack.
   OPTIONAL (demoted): an AOT-compiled client only serves a human with NO shell-capable agent; a
   decision to skip it entirely is acceptable and the copy-paste prompt must not depend on it.
4. **Show the human's principal id on the page** — display "Your account id" with a copy button
   (from `GetCurrentSession.Response.PrincipalId`, already claimed by the auth provider as
   `timewarp:principal_id`) so the copy-paste prompt can itself carry the human's id without leaving
   the page. Keep Profile/Settings fingerprint-only if preferred; the minimum viable change is this
   page.

## Checklist

- [x] Page copy: what linking means (open agent links page onboarding section)
- [x] Copy-paste agent prompt block: pure openssl + curl recipe (copy button; no dotnet/SDK dependency)
- [x] AOT client: explicit SKIP-or-ship decision recorded in the kitchen (agent targets proven not to need it)
- [x] Surface the human's PrincipalId on AgentLinksPage with copy support
- [x] Playwright test coverage on the web-spa page changes
- [x] `dev build`, `dev template-smoke` per PR gates in AGENTS.md

## Decision

AOT client: **SKIP**. A shell-capable agent finishes key registration, token issuance, and `POST api/agent-links` with openssl and curl (proven live 2026-10-28, session ses_e7d6c83cfffebh1UD5myX7NDeE). The page prompt is that recipe. `tools/agent-identity-cli` stays maintainer-only and does not pack. An AOT binary would only help a person with no shell-capable agent, which this task does not serve.

## Results

The Agent links page explains what a link is, shows the signed-in account id, and copies a prompt the person can paste to their agent. The id is the `timewarp:principal_id` claim from `GetCurrentSession` (mock auth carries the same claim). Copy buttons dispatch `AgentLinksState.CopyOnboarding`, which writes the clipboard. The prompt is a bash recipe: P-256 keygen, register (`TimeWarp.Identity.AgentKey.Register.v1:`), token with `identity:read` (`Token.v1`), then `POST api/agent-links` with this account id. No packed client ships.

### How to validate

Smoke: Sign in, open `/AgentLinks`, read "What linking means" and "Your account id", then click Copy and Copy prompt.

Expect: The explanation contains `Paid service does not require a linked human.` and says you approve or deny, then the agent can fetch the `timewarp.humanUx/v1` handoff (`GetHumanUx`). The account id is the session principal guid. The prompt includes that id, the server origin, `openssl genpkey` for P-256, both ceremony prefixes, `identity:read`, and `POST api/agent-links`. Each copy button writes that text to the clipboard. Playwright `AgentLinksOnboarding_Given_Wasm.Page_Should_ShowTheAccountId_And_CopyThePrompt` passed. `dev build` completed with 0 warnings and 0 errors. `dev template-smoke` passed (exit 0, Template smoke SUCCEEDED): SmokeDefault built with 0 warnings, the initializer import graph was OK, and the smoke suites passed (create-role 5/5, get-weather-forecasts 5/5, hello 2/2, web-jaribu-tests 441/441, api-jaribu-tests 9/9, timewarp-testing-tests 14/14).

## Notes

- Server contracts touched (none expected): `RequestAgentHumanLink`, `GetHumanUx` under
  `source/container-apps/web/features/agent-links/`. Page:
  `source/container-apps/web/projects/web-spa/features/agent-links/pages/AgentLinksPage.razor`.
- Candidate follow-up: human-initiated link-code flow (no raw principal id shared) — recorded here
  so it is not lost, not scheduled.
- The live request attempt against the dev server returned `404 Human not found` for
  `MockUserIds.SystemAdmin` — mock ids exist only for SPA mock auth; reaffirms requirement 4.

## Session

- Created: ses_e7d6c83cfffebh1UD5myX7NDeE (2026-10-28)
- Implementation: grok task-work (2026-10-28)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18301-4e35-7d91-9082-f95316c1078f, max-turns 500, Done — 2026-10-28T15:13:46Z
