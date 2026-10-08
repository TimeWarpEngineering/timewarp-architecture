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

- [ ] Domain model and persistence for a feedback item (owner, kind, title, body, id)
- [ ] Submit returns the id and permalink
- [ ] Owner-only read, plus the filer's own list
- [ ] Optional email copy from `Profile.Email`, off by default
- [ ] Tests in Requirements item 7

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
- Captured only. No product code.

## Results

*(fill when implementation is done)*

### How to validate

**Smoke:** sign in, file one complaint with email-copy off, then one with it on for a profile that has an email, and one for a profile with no email.

**Expect:**

- Each submit shows an id and a link, and the link opens that item.
- A second user cannot open the first user's link.
- Mail goes out only for the opted-in filing that had an email on the profile.
- The no-email filing still succeeds.
