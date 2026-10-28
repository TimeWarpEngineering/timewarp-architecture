# Improve agent links page onboarding

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

- [ ] Page copy: what linking means (open agent links page onboarding section)
- [ ] Copy-paste agent prompt block: pure openssl + curl recipe (copy button; no dotnet/SDK dependency)
- [ ] AOT client: explicit SKIP-or-ship decision recorded in the kitchen (agent targets proven not to need it)
- [ ] Surface the human's PrincipalId on AgentLinksPage with copy support
- [ ] Playwright test coverage on the web-spa page changes
- [ ] `dev build`, `dev template-smoke` per PR gates in AGENTS.md

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
