# Add user feedback filings with an id, permalink, and optional email copy

## Description

A signed-in user of the TimeWarp Architecture web app can file feedback and keep proof that it landed. Kinds are bug report, feature request, complaint, and other.

The failure this closes: someone complains, they are told it will be filed, and nothing comes back. A thank-you with no id is not done.

This task is the filing and the receipt. Do not implement an assistant that files on the user's behalf.

## Steve's direction (2026-10-09 ~02:00 ICT, overnight run)

Steve asked for this task to be **implemented and merged tonight** (serial 272 → 271 → 273; 271's agentic UI / WebMCP catalog-to-tools mapping is on master before this starts), and added:

> "WebMCP should allow an agent to drive the functionality and the AI integration should be able to also."

This **supersedes** the 2026-10-03 lines "Do not implement an assistant that files on the user's behalf" and "Not in scope: An assistant, Grok, or other agent filing on the user's behalf" — for the mechanism only:

- Expose **submit feedback**, **list my feedback**, and **open feedback by id** as TimeWarp.State `[CatalogAction]`s visible to agents (Visibility `Both`), so both the in-app AI integration (task 271) and WebMCP browser agents can drive them through the same catalog-to-tools mapping 271 built.
- Agent-driven calls run **as the signed-in user**, with the same server authorization (owner-only reads; unsigned cannot submit). Submit is consequential, so it is approval-gated per 271's approval rule. The agent gets the id and permalink back in the result.
- Do not build a new assistant or a separate filing bot; this is just the actions being agent-callable.
- Still out of scope: admin inbox, assignment/status workflow, public board, attachments, voting, comments, new profile/email field.

**Email:** the repo has no email-sending infrastructure yet. Add a minimal sender abstraction with a development implementation (log / pickup-directory) and no real credentials or required secrets; the "email me a copy" behavior and tests run against a fake. Note in Results what a real provider would need.

**Proof in the PR body (Steve's standing rule for UI work):** test output (Requirements item 7 plus agent/WebMCP tool coverage), build log summary, and a screenshot or captured log of a filing showing the id and permalink.


## Requirements

1. A signed-in user submits one item: kind, title, and body. Unsigned users cannot submit.
2. Submit assigns a stable id and returns it in the same response. The UI shows that id and a permalink to the item immediately. Do not show only "thanks".
3. The permalink opens that same item later for the filer. Another signed-in user cannot read it.
4. The filer can list their own items and open any of them from that list.
5. Email is optional and uses the existing profile email (`Profile.Email` on `source/container-apps/web/features/profile`). Do not add a second address store.
   - If the profile has an email, the form offers "email me a copy", off by default.
   - Checked: send one message containing the id, kind, title, body, and permalink.
   - Unchecked, or no email on the profile: submit still succeeds and no mail is sent. Do not ask the user to add an email as part of filing.
6. The item is stored in the app's existing persistence and is still there after a restart. The chat transcript is not the record.
7. Tests cover: id returned, permalink resolves for the owner, another user cannot read it, mail sent only when opted in and an email is on file, submit succeeds with no email.

## Checklist

- [x] Domain model and persistence for a feedback item (owner, kind, title, body, id)
- [x] Submit returns the id and permalink
- [x] Owner-only read, plus the filer's own list
- [x] Optional email copy from `Profile.Email`, off by default
- [x] Tests in Requirements item 7

## Notes

Motivation (2026-10-03): when a user complains that an assistant did not do something, the assistant says it will file the complaint and the user never hears an id back. The acceptance bar is the receipt.

Existing profile email is optional progressive profile. Passkey register does not require it. This task must not make email required for filing or for sign-in.

### Not in scope

- Admin inbox, assignment, status workflow, or a public board
- An assistant, Grok, or other agent filing on the user's behalf
- Attachments, screenshots, voting, or comments
- A new profile or a new email field

## Session

- Created: 59800 (2026-10-03)
- Implemented: 2026-10-09. Filing, receipt, owner-only read, optional profile-email copy, and the three catalog actions (`Feedback.SubmitFeedback`, `Feedback.ListMyFeedback`, `Feedback.OpenFeedback`) at Visibility `Both`. Submit stays approval-gated. No new assistant.

## Results

A signed-in user can file one feedback item (bug report, feature request, complaint, or other) and gets a stable id and a relative permalink `/Feedback/{id}` in the same response. The list page shows that id and link immediately (`data-qa="FeedbackReceiptId"` and `data-qa="FeedbackPermalink"`). The permalink opens the same item for the filer. Another signed-in user gets not-found. The filer can list their own items and open any of them.

Email copy uses `Profile.Email` through `IProfileEmailLookup`. The checkbox is offered only when that address is present, and it is off by default. Checked plus an address sends one message (id, kind, title, body, absolute permalink). Unchecked, or no address, still succeeds and sends nothing. Filing does not ask the user to add an email.

The three actions are agent-callable through the existing catalog-to-tools mapping. Calls run as the signed-in user. Submit is consequential, so it waits on the in-app approval gate. The tool result includes the id and permalink. Unsigned callers cannot submit. There is no separate filing bot.

Persistence is the existing dual store. With a Postgres connection string, `EfFeedbackStore` and migration `20261009001245_AddFeedbackItems` (schema `feedback`, table `feedback_items`, plus `feedback.file.self` on Member, Operator, Administrator, and Developer) keep the row across a process restart. With no connection string the in-memory store is the record for that process only; it does not survive a restart. The chat transcript is not the record.

Mail is `IEmailSender` plus `DevelopmentEmailSender`. It logs every message and, when `Mail:PickupDirectory` is non-empty, writes one `.eml` file. No credentials and no provider SDK are committed. `appsettings.Development.json` leaves `Mail:PickupDirectory` empty. A real provider needs an SMTP or HTTP API host, a credential that stays out of the repo, a from-address (the message type has none; the development pickup file uses a fixed from-line), and a production `IEmailSender` registration in place of `DevelopmentEmailSender`.

The interactive browser filing was not driven. Sign-in is passkey, and the Aspire host from the catalog suite is not left running. Proof is the captured logs below. The receipt markup is on `FeedbackListPage` and `FeedbackItemPage`.

Build: `dotnet build source/container-apps/web/projects/web-server/web-server.csproj --nologo -v q` — Build succeeded. 0 Warning(s). 0 Error(s). Time Elapsed 00:00:02.64. That project references the SPA, so the filing pages are in this build. The catalog suite also started the Aspire AppHost and served the web server.

### How to validate

**Smoke:** sign in, file one complaint with email-copy off, then one with it on for a profile that has an email, and one for a profile with no email.

Commands that already passed on 2026-10-09:

- From `tests/container-apps/web/web-jaribu-tests`: `dotnet test -- --filter-class FeedbackFiling` — 15 passed. Console:
  - `FILING-PROOF id=01a11e06-2282-7518-ba03-25dc09bedef4 permalink=/Feedback/01a11e06-2282-7518-ba03-25dc09bedef4`
  - `FILING-PROOF id=01a11e06-22ae-78b9-91f5-0f95437c8490 permalink=/Feedback/01a11e06-22ae-78b9-91f5-0f95437c8490 emailCopySent=true`
- Same directory: `dotnet test -- --filter-class PermissionIds` — 8 passed. `dotnet test -- --filter-class PermissionEvaluator` — 17 passed.
- From `tests/container-apps/web/web-infrastructure-tests`: `dotnet test -- --filter-class Feedback` — 3 passed (model mapping plus a Postgres round-trip that applied `20261009001245_AddFeedbackItems`).
- From `tests/container-apps/web/web-spa-integration-tests`:
  - `dotnet test -- --filter-class CatalogAgent` — 16 passed. Console includes `WEBMCP-PROOF path=/Feedback tools=Feedback.SubmitFeedback,Feedback.ListMyFeedback,Feedback.OpenFeedback,page_context receipt={"action":"Feedback.SubmitFeedback","completed":true,"result":{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","permalink":"/Feedback/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"}}`
  - `dotnet test -- --filter-class ActionCatalog` — 10 passed.
  - `dotnet test -- --filter-class CommandPalette` — 35 passed.
  - `dotnet test -- --filter-class SignOut_Should` — 2 passed.

**Expect:**

- Each submit shows an id and a link, and the link opens that item.
- A second user cannot open the first user's link.
- Mail goes out only for the opted-in filing that had an email on the profile.
- The no-email filing still succeeds.
- On `/Feedback`, WebMCP lists `Feedback.SubmitFeedback`, `Feedback.ListMyFeedback`, `Feedback.OpenFeedback`, and `page_context`. An approved submit returns the id and permalink in the tool result. List and open are not approval-gated. Submit is.
- Sign-out clears `FeedbackState`. The command palette lists the `/Feedback` page and the parameterless `Feedback.ListMyFeedback` command for a principal who can file. `Feedback.SubmitFeedback` and `Feedback.OpenFeedback` stay out of the palette because they take arguments. A principal without `feedback.file.self` sees neither the page nor the list command.
