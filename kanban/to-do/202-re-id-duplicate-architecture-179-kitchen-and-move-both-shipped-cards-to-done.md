# Re-id duplicate architecture 179 kitchen and move both shipped cards to done

## Description

`ganda reposet show live` shows **two** in-progress architecture rows both numbered **179**.
That is origin-home, not a display bug. Overlay (ganda **220**) keeps duplicate home ids.

Both product changes already merged to master on 2026-08-12; kitchens stayed in
`kanban/in-progress/`.

| File | Product | PR |
|------|---------|----|
| `kanban/in-progress/179-home-sign-in-cta-link-to-login-only-no-ceremony-clone.md` | `b174e6e9` home Sign in CTA | **301** (via `dev`) |
| `kanban/in-progress/179-silence-globalusingsanalyzer-on-shebang-git-hooks-post-mergepost-commit.md` | `2457d800` githooks analyzer silence | **300** |

## Requirements

Kanban-only. Do not change product code.

1. **Keep** the home Sign-in kitchen as **179**. `git mv` it to `kanban/done/`.
2. **Re-id** the githooks-silence kitchen: `ganda kanban reserve` (prints id only — do
   **not** hand-number). Rename the file to `kanban/done/{new}-silence-globalusingsanalyzer-on-shebang-git-hooks-post-mergepost-commit.md`.
   Update the `#` title if needed. Add a Notes line: formerly duplicate id 179; product PR **300**.
3. Add `## Results` + `### How to validate` on **179**, **{new}**, and **this** task if missing
   (Smoke + Expect). Check off leftover checklist items that shipped.
4. This kitchen must be in `kanban/done/` in the PR.
5. After: `reposet show live` / `ganda kanban` must not show two architecture 179s in-progress.
6. PR; STOP. Do not merge.

Do not implement on origin-home. Stay in this claim worktree.

## Checklist

- [ ] 179 home Sign-in moved to `kanban/done/` (id unchanged)
- [ ] Githooks silence re-id via `ganda kanban reserve` into `kanban/done/`
- [ ] Results + How to validate on both shipped kitchens + this task
- [ ] This kitchen in `done/`; PR; STOP

## Session

- Created: 1291218 (2026-08-26)
- Cockpit: Grok 01a0275a — duplicate 179 on `reposet show live`

## Notes

Neighboring ids 180–201 already exist. CAS reserve is required for the new id.
`ganda kanban who 179` is unclaimed (both files). Overlay will show one 179 after
the rename because only one file will parse as 179.
