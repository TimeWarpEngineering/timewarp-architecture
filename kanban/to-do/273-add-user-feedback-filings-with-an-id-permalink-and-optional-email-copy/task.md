# Add user feedback filings with an id, permalink, and optional email copy

## Description

A signed-in user of the TimeWarp Architecture web app can file feedback and keep proof that it landed. Kinds are bug report, feature request, complaint, and other.

The failure this closes: someone complains, they are told it will be filed, and nothing comes back. A thank-you with no id is not done.

This task is the filing and the receipt. Do not implement an assistant that files on the user's behalf.

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
