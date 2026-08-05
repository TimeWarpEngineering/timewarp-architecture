# Remove obsolete home-page Freeze Team hero credits

## Description

Home page still shows a "Credits" card attributing hero artwork to The Freeze Team.
Hero artwork is no longer used; remove that card.

## Requirements

1. Remove the Credits card from `HomePage.razor`.
2. Do not remove the nav-menu Freeze Team blog link (still a valid external link).
3. Leave `wwwroot/images/the-freeze-team/` alone unless a follow-up wants dead-asset cleanup.

## Checklist

- [x] Create task / move in-progress
- [x] Remove Credits card from HomePage
- [x] Commit

## Results

Removed the home-page Credits card (hero artwork attribution to The Freeze Team).

### Changed

- `source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor` — deleted Credits `Card`

### How to validate

**Smoke**

1. Open Home.
2. Expect cards: Welcome, Built with, Sign in, Try it — **no** Credits / Freeze Team hero attribution.

**Automated**

- None (markup-only).

## Session

- Implement: grok (2026-08-05)
