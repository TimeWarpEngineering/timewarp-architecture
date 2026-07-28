## Round 1 — general

**Date:** 2026-07-27
**Scope:** commits `351959b5` (seam moves, abstractions retired) + `c7d31c07` (placement guide:
skill opening, AGENTS.md compression), plus current tree.

## Summary

Both commits do exactly what task.md and the review framework specify. Empirically verified
compilation-unit membership, namespace preservation, the `i-request-host-accessor` platform
classification, the accuracy of the new skill's decision table against the live tree, and a
repo-wide sweep for stale `abstractions/` references. No issues found.

### 1. Seam moves

- `git show 351959b5 --stat` confirms pure renames (100% similarity, 0 insertions/deletions) for
  all four files: `i-agent-caller-context`, `i-browser-session-service`,
  `i-current-principal-accessor`, `i-request-host-accessor`, each moving from
  `web-application/abstractions/` to `platform/identity-host/` with the `-application.cs` suffix
  appended.
- `web-application/` now contains only `web-application.csproj` and `global-usings.cs` (plus
  bin/obj build output) — the `abstractions/` folder is gone, confirmed by directory listing.
- `dotnet build source/container-apps/web/web-application/web-application.csproj -getItem:Compile`
  lists all four moved files as `Compile` items, defined by
  `feature-filename-grammar.g.props` — the `-application.cs` suffix glob is pulling them into the
  web-application compilation unit from their new physical location, exactly as designed.
- Namespaces unchanged: all four files still declare `namespace TimeWarp.Architecture.Abstractions;`
  (grepped directly). No renames occurred.
- `i-request-host-accessor` classification checks out against real usage: its only consumers are
  `IRequestHostAccessor` references in the WebAuthn/passkey handlers under
  `features/identity/{start,complete}-passkey-{registration,authentication}/` and
  `add-passkey/`, plus DI registration in `web-server/program.cs`. Its implementation
  (`HttpRequestHostAccessor`) already lives in `platform/identity-host/http-request-host-accessor-server.cs`.
  identity-host is the correct cluster.
- Purpose/Design regions of all four moved files were inspected for stale folder narration
  (task.md flagged this as a required reconciliation step). None of the four regions mention the
  old `web-application/abstractions/` location at all — they describe the seam/port pattern in
  purely conceptual terms (scheme-agnostic reads, sync-vs-async rationale, etc.), so there was
  nothing to reconcile. Consistent with the commit being a pure rename (0 content diff).
- Repo-wide grep of `identity-host/*.cs` for the string `abstractions` returns nothing — no
  internal stale references either.

### 2. Placement guide quality

- `skills/tw-feature-placement/SKILL.md` opens with the one-sentence rule and the litmus test
  verbatim as quoted in task.md, followed immediately by the three-row decision table
  (`web/features/<slice>/`, `web/platform/<cluster>/`, artifact folder) — before the filename-grammar
  detail, as required.
- Table accuracy spot-checked against the live tree:
  - `admin/roles/create-role/` exists under `web/features/admin/roles/`.
  - `chat/chat-hub-server.cs` exists at `web/features/chat/chat-hub-server.cs` — slice root,
    above any use-case folder, matching the table's claim that it's a shared/whole-slice file.
  - `platform/identity-host/i-current-principal-accessor-application.cs` +
    `http-current-principal-accessor-server.cs` both exist side by side, as cited.
  - `web-server/configuration/sample-options.cs` exists; its Purpose region reads "Example
    options class demonstrating the AddFluentValidatedOptions binding-plus-validation pattern" —
    consistent with the table's characterization "(binding/validation exemplar, not a real
    concern)".
- Style: present tense throughout, no task numbers or dates, no history narration in the main
  prose. The one mention of the old `abstractions/` folder ("the old `web-application/abstractions/`,
  retired: conflating layer with folder was never a principled reason...") is a brief backward
  reference used to justify *why* the current rule is what it is — it reads as reasoning
  ("here's why we don't do that"), not as a changelog entry or task narration. This is a judgment
  call the task explicitly asked the reviewer to weigh in on; I read it as staying on the right
  side of the line — it's one clause, framed as the rejected alternative to the stated rule, not a
  historical account of what happened when.
- AGENTS.md: new paragraph is accurate, and it's a minimal addition (7 lines) inserted right
  after the Layout tree diagram, restating the rule + litmus test compactly and pointing to the
  skill. The following Axis-1 paragraph (grammar/registry/namespaces) is undisturbed except for the
  last sentence being extended to mention "rule, litmus test, and decision table" — no
  contradiction between the two paragraphs; they're complementary (new paragraph = placement
  decision, existing paragraph = grammar mechanics once placement is known).

### 3. Sanity-test reproductions (2 verified directly)

- `chat-hub-server.cs` — confirmed at slice root (`web/features/chat/chat-hub-server.cs`), above
  any use-case folder, reproducing the claimed "product concern serving more than one operation
  stays at slice root" placement.
- `sample-options.cs` — confirmed its own Purpose region self-describes as an example/exemplar
  ("Example options class demonstrating..."), reproducing the claimed "not a real concern, fails
  the litmus test, stays with the host artifact" placement.

### 4. Stale-reference sweep

- `grep -rn "web-application/abstractions"` and `grep -rln "abstractions/"` repo-wide (excluding
  `kanban/`) each return exactly one hit outside kanban/: the new SKILL.md's own backward-reference
  clause discussed above. All other hits are in `kanban/done/*` or `kanban/in-progress/126-011/*`
  task/review files, which are historical records of prior tasks and out of scope for this sweep
  per the task's own framing (this task's spec itself references the old path descriptively).
- `tw-slice-isolation/SKILL.md` and `tw-aggregate-pattern/SKILL.md` both already point to
  `tw-feature-placement` for filename-grammar/layer-membership detail rather than restating
  placement rules (confirmed via grep) — the implementer's claim that "no changes were needed
  there" holds.

### 5. `web-infrastructure-module.cs`

Confirmed untouched: still at `source/container-apps/web/web-infrastructure/web-infrastructure-module.cs`,
not present in either commit's diff, and its `git log` history shows only unrelated feature
commits (104-003/104-004, EF persistence, etc.) — no move happened, consistent with "maintainer
ruling pending."

### 6. Empirical spot-check

`-getItem:Compile` evaluation on `web-application.csproj` performed (see item 1) — all four moved
files present in the Compile item group via the `-application.cs` glob, defining project
`feature-filename-grammar.g.props`, confirming the compilation-unit split survived the move.

## Issues

None.
