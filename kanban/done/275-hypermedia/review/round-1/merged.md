# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general, security, tests

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 3 |
| nit | 0 | 2 | 2 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:242
- Description: Row `@key` was `{Kind}:{Target}`. Contextual rows share Target (every C row is `HypermediaLab.FollowCommand`; B repeats `Credentials.RevokeCredential` per credential), so sibling keys collide and Blazor throws on render on the lab page. Headless tests do not render the list.
- Suggestion: key on the full row.
- Source: general
- Disposition notes: Fixed — `@key=row` (record equality: Kind, Target, ArgumentsJson, … — the same identity `IsOffered` uses). Verified by reading the code; the palette list has no render test (see M7).

### M2 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/hypermedia-lab/hypermedia-lab-context-source.cs:24
- Description: B and C payloads refresh independently; after a B revoke the C tab / "Lab C" rows are stale until C refetches; only the server 409 stops a stale C revoke.
- Suggestion: refresh both payloads after any lab action, or contribute only the active tab's rows.
- Source: general
- Disposition notes: wontfix (orchestrator). Inherent to running two approaches side by side over one data set; each approach shows its own latest payload, which is what Requirement 3 asks. Adoption keeps one approach, so the cross-approach staleness disappears. The server backstop (409) holds. Recorded in the comparison Notes.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-ranker.cs:11
- Description: `Contextual` became the first enum member (sorts first); ranker Design region still said "pages first, then commands".
- Suggestion: reconcile the Design region.
- Source: general
- Disposition notes: Fixed — Design now says contextual rows first, then pages, then commands.

### M4 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-runner.cs:84
- Description: Contextual rows skip catalog Permissions/Visibility and there is no client allow-list of offerable names; any catalog entry a payload names would run.
- Suggestion: check Visibility (Human/Both) and Permissions, or an offerable-names allow-list, before Execute.
- Source: security
- Disposition notes: wontfix for the lab (orchestrator). Already a documented decision in the runner Design region (server decides the offer and re-enforces on the real endpoint). Added to the comparison Notes as adoption hardening for B, and the "second allow-list" claim is now qualified.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/contextual-action-arguments.cs:65
- Description: An explicit JSON `null` satisfied a required reference-typed parameter.
- Suggestion: treat null as missing for required parameters.
- Source: security
- Disposition notes: Fixed — null counts as absent (required → refused; optional → skipped); Design region updated; new `{"credentialId":null}` input on `B_Fail_Closed_On_Arguments_That_Do_Not_Bind` (lab suite 24/24).

### M6 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/hypermedia-lab/app-relative-href.cs:17
- Description: The guard allows any same-origin path, not only `/api/`.
- Suggestion: restrict to the API prefix.
- Source: security
- Disposition notes: wontfix (orchestrator). C's `NAVIGATE` targets the browser-protocol Entra challenge path, which is not under `/api/`; narrowing would break the Link Microsoft 365 command. The same-origin-only reach is already called out as C's weaker security model in the comparison (item 4).

### M7 — Severity: suggestion — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/hypermedia-lab/hypermedia-lab-tests.cs
- Description: Nothing renders `HypermediaLabPage`; "offered actions appear as buttons" is covered only indirectly through the runner and `HypermediaLabRows`.
- Suggestion: per-tab render test, or drop the claim.
- Source: tests
- Disposition notes: wontfix (orchestrator). The repo has no component-render harness (no bUnit); the page builds its buttons from the same `HypermediaLabRows` the tests drive, and the browser check is explicitly recorded as not performed. An evaluation lab does not justify introducing a render-test framework.

### M8 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-server-integration-tests/features/hypermedia-lab/hypermedia-lab-endpoint-tests.cs
- Description: Server suite posts only the Revoke link end to end; the Rename link (href + nickname/userId body) is never posted to the real endpoint.
- Suggestion: add a Rename follow test.
- Source: tests
- Disposition notes: wontfix (orchestrator). Rename's href is pinned as app-relative and built from the generated route; the SPA suite covers Rename with input. Worth adding only for the adopted approach.

## Duplicates / conflicts

- None: the three reviewers' findings did not overlap. All three independently confirmed `CredentialRules` reuse in `RevokeCredential.Handler` / `EntraTicketProcessor` is behaviour-identical to master.
