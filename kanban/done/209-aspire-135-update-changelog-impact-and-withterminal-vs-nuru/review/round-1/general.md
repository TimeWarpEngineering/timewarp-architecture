# Round 1 — general
**Date:** 2026-09-02
**Scope reviewed:** branch task/209… vs master (b19ee730)

## Summary

The 13.5.2 → 13.5.3 pin bump is clean and complete (SDK, CPM hosting pins, EF preview, and the
hand-aligned `Aspire.Hosting.Testing` are all on one train; no CPM version overrides bypass it).
The `.gitignore` and `dev-cli` claims check out. The skill-prose reconciliation is mostly accurate
but left one duplicated line the edit itself introduced, plus a few now-inconsistent `aspire ps`
"resource list" statements in the very files that were edited for this reconciliation (touched
section fixed, adjacent sections in the same file not caught). No version bump is required for
this PR under `dev check-version` policy (already ahead of the latest tag).

## Verified claims

- Pins on one 13.5.3 train (`Directory.Packages.props`, `aspire-app-host.csproj`; EF preview
  `13.5.3-preview.1.26425.3`) — **pass**, confirmed by direct read; no 13.5.2/13.4 leftovers found
  in any `.props`/`.csproj`/`global.json` under `source/`, `tests/`, or `timewarp-templates/`. No
  hardcoded `PackageReference` Version or `VersionOverride` bypassing CPM for any `Aspire.*` id.
- `dev db *` uses `aspire resource web-migrations <cmd> --apphost … --non-interactive --nologo`,
  never `aspire ps` — **pass**, confirmed in `tools/dev-cli/endpoints/db-app-host.cs`.
- `.claude/skills` and `.agents/skills` trees byte-identical — **pass**, `diff -r` reports no
  differences.
- No tracked file under `.memsearch/` — **pass**, `git ls-files .memsearch` is empty; the new
  `.gitignore` entry `.memsearch/memory/` only affects future untracked writes.
- No `ServiceProvider`, `PublishAsConnectionString`, `WithTerminal`, `AddDotnetProject`, or
  `TerminalOptions` anywhere under `source/container-apps/aspire/projects/aspire-app-host/` —
  **pass**, grep returns zero hits, matching the Results table (items 1, 2, 6, 9, 10).
- No `13.4` prose left anywhere in `.claude/skills` / `.agents/skills` — **pass**.
- `aspire/references/aspire-13-3-breaking-changes.md` still 13.3-only, noted (not fixed) as future
  work — **pass**, file confirmed present under that exact name.
- Version policy: `source/Directory.Build.props` `<Version>` is `2.0.0-beta.17`; latest tag is
  `v2.0.0-beta.16`. Source version is already newer than the latest tag, so `dev check-version`
  should pass without a bump for this PR (Aspire is a third-party pin, not a
  `TimeWarp.Foundation.*`/`Architecture.*`/`Identity` platform package, so task-124's
  pins-equal-`<Version>` rule doesn't apply here either). No Estimate fields or time-estimate
  language found in task.md.

## Issues

### Issue 1 — Severity: bug
- File: `.claude/skills/aspire-orchestration/references/safety-guardrails.md:233-235` (mirrored at `.agents/skills/aspire-orchestration/references/safety-guardrails.md:233-235`)
- Description: The edit that changed `aspire ps --include-hidden --format Json` to
  `aspire describe --include-hidden --format Json` left the pre-existing next line unchanged, so
  the code block now reads:
  ```
  # ✅ Debugging / completeness — include hidden resources
  aspire describe --include-hidden --format Json
  aspire describe --include-hidden --format Json
  ```
  a literal duplicate line, in both mirrored skill trees.
- Suggestion: Delete the redundant second line.
- Status: open

### Issue 2 — Severity: suggestion
- File: `.claude/skills/aspire-orchestration/references/safety-guardrails.md:198,213` and `.claude/skills/aspire-orchestration/references/resource-management.md:15` (both mirrored under `.agents/skills/...`)
- Description: These lines still describe `aspire ps --format Json` as returning resource-level
  `name`/`displayName` fields ("✅ Machine-readable resource list" / "`aspire ps --format Json`
  returns `name` and `displayName` fields" / "Use `displayName` from `aspire ps --format Json`,
  not `name`"). That contradicts the 13.5 semantics this same commit documents two sections later
  in `safety-guardrails.md` itself ("13.5 `aspire ps` lists running AppHosts, not resources") and
  in `aspire-monitoring/SKILL.md`. These are exactly the kind of "leftover stale reference the
  edit missed" the task asked to check for — the Hidden-Resources section of these files was
  patched but the adjacent Rule 5 / resource-management guidance in the same files was not.
- Suggestion: Point these at `aspire describe --format Json` (or `aspire resource`) for
  resource-level `name`/`displayName`, consistent with the fix already made elsewhere in the same
  files.
- Status: open

### Issue 3 — Severity: nit
- File: `kanban/to-do/209-aspire-135-update-changelog-impact-and-withterminal-vs-nuru/task.md` checklist item "`dev build` 0/0; aspire-tests still boot"
- Description: The checklist item is checked, but the Results section records no actual run
  output for `dev build` or `aspire-tests` (e.g. warning/error counts, pass/fail totals) — only
  the "How to validate → Expect" section, which is prescriptive guidance for a future
  verifier, not a record that the implementer ran it. Everything else in Results is backed by
  concrete grep/read evidence; this one checklist line is not.
- Suggestion: Either add the actual `dev build` / `aspire-tests` run evidence to Results, or note
  explicitly that it's deferred to the orchestrator's separate build/test pass.
- Status: open
