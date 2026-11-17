# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** same as framework (proposal.md, task.md, `skills/tw-aggregate-pattern/SKILL.md`)

## Summary

The spike meets every acceptance item. It inventories all 20 handlers (7 Roles, 13 Feedback; file
counts verified). Options A–D are written as code and use metadata only. Steve's decision (option A)
is recorded, the skill fold-in is in place, and follow-ups 477–479 are on master in `to-do`. I
re-checked these claims against the repo: Mapperly is referenced only at `Directory.Packages.props:90`
and `web-application.csproj:4`, `FeedbackContractMap` exists, and the Role mutation signatures match
the proposal. `dev lint-skills` passes (9/9). The goal restatement matches the task, so there is no
misread. The skill text has two accuracy problems, and the kitchen cites one test wrongly.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-aggregate-pattern/SKILL.md:111
- Description: The named-argument example is `new Response(RoleId: role.Id.Value, Name: role.Name, …)`.
  This repo's contract constructors take camelCase parameters
  (`get-role-contracts.cs:67-71`: `roleId`, `name`, `description`, …). C# named arguments are
  case-sensitive, so the example fails with CS1739 on the type it is modeled on. The skill ships
  to generated apps, and agents copy its examples. The proposal's own option A code
  (`proposal.md:117`) uses the correct `roleId:`.
- Suggestion: Write `new Response(roleId: role.Id.Value, name: role.Name, …)`.
- Status: open

### Issue 2 — Severity: suggestion
- File: skills/tw-aggregate-pattern/SKILL.md:120
- Description: "Mutation calls pass the principal and the clock" is stated as a universal rule.
  The Role aggregate's mutations take neither (`role-domain.cs:53,71,79,103`: `Create(name,
  description)`, `UpdateDetails`, `SetPermissions`, `SetAgentScopes`). The proposal counts 3 of
  those calls as purely mechanical. An agent reading the rule could "fix" Role by adding parameters
  it doesn't need, or decide the Roles handlers are wrong.
- Suggestion: Make the rule conditional. For example: "When the mutation records who acted or
  when, the handler passes the acting principal and the clock; the root does not read them from
  ambient context." Keep the `Can…` check and status-code-rule sentences.
- Status: open

### Issue 3 — Severity: nit
- File: kanban/in-progress/476-…/proposal.md:81 (also task.md Results, "Findings" bullet)
- Description: The proposal cites `feedback-admin-tests.cs:263` and `:420` as the tests that
  assert the filer-thread redaction. Line 263 is a handler check. Line 420 is
  `MockFactories_Should_ProduceDeterministicSamples` and checks the mock response factory, not the
  handler projection. The real handler and domain checks are `feedback-admin-tests.cs:262-263` and
  `:324`, and `feedback-triage-tests.cs:177` (`InternalNote_Should_BeHiddenFromTheFiler`).
- Suggestion: Replace `:420` with `:324` (and optionally `feedback-triage-tests.cs:177`) in both
  places.
- Status: open
