# Round 2 — general
**Date:** 2026-11-05
**Scope reviewed:** round-1 fix delta `034e0ea22..f0a4acae4`: `skills/tw-aggregate-pattern/SKILL.md`
(named-argument example and the "handler supplies who and when" bullet), `proposal.md` redaction
citations, `task.md` Results, Session, and Review.

## Summary

The fix pass changes documentation only. I checked the round-1 findings against the repo:

- **M1 is fixed.** The example now reads `new Response(roleId: role.Id.Value, name: role.Name, …)`.
  This matches the constructor parameters in
  `source/container-apps/web/features/admin/roles/get-role/get-role-contracts.cs:67-68`.
- **M2 is fixed.** The bullet now says the handler passes the principal and the clock only when the
  mutation records them, and the root does not read ambient context. This fits the Role mutations,
  which take neither.
- **M3 is fixed.** The citations now point at `feedback-admin-tests.cs:263`
  (`filerView.Activity.ShouldAllBe(entry => entry.AuthorPrincipalId == null)`),
  `feedback-admin-tests.cs:324` (`filerView.Activity.Single().AuthorPrincipalId.ShouldBeNull()`),
  and `feedback-triage-tests.cs:177` (`InternalNote_Should_BeHiddenFromTheFiler`). Each one asserts
  what the proposal says it asserts.

The task's Results and "How to validate" match the new skill text. `dev lint-skills` passed 9 of 9
skills, run through the runfile `dotnet run tools/dev-cli/dev.cs -- lint-skills`.

`## Goal (implementer restatement)` matches the Description and Requirements, so there is no misread.

## Issues

None.
