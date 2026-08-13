# Add tw-blazor skill for razor file order

## Description

Capture the `.razor` file-order convention (one `@code` at the top, markup, optional
`<style>` last) as `skills/tw-blazor`, sibling to `tw-blazor-css-strategy` and
`tw-blazor-layout`. Register it as a ganda skill source.

## Checklist

- [x] Write `skills/tw-blazor/SKILL.md`
- [x] `ganda skills add` + `ganda skills sync`
- [x] Commit

## Notes

Source URI uses this `dev` worktree until the skill is on `master`
(siblings already point at `master`).

## Results

`skills/tw-blazor/SKILL.md` is the SSOT. Registered and synced:

```text
ganda skills add tw-blazor worktree://github.com/TimeWarpEngineering/timewarp-architecture/dev/skills/tw-blazor
ganda skills sync
```

### How to validate

```bash
ganda skills list
# Expect: tw-blazor → worktree://…/timewarp-architecture/dev/skills/tw-blazor

test -f skills/tw-blazor/SKILL.md && echo ok
# Expect: ok

rg -n "Never two \`@code\`" skills/tw-blazor/SKILL.md
# Expect: one match
```

## Session

- Implementation: grok 2026-08-13
