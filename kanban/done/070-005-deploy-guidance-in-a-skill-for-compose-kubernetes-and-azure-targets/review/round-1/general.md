# Round 1 — general
**Date:** 2026-10-07
**Scope reviewed:** master...HEAD (skills/tw-deploy/SKILL.md, AppHost Design region, template-smoke harness, AGENTS.md)

## Summary
Every falsifiable claim in skills/tw-deploy/SKILL.md was checked against the AppHost (program.cs, constants.cs), tools/dev-cli (publish-compose/kubernetes commands, aspire-publish.cs) and aspire-tests. All of them matched. Verified: the Publish:Target values and the throw on unknown values, WithDashboard(enabled: false) on both environments, all parameter and resource names, Helm >= 4.2, the migration commands (identical to the Design region), output paths, the safety suites, helm lint, and the 5 MiB Helm limit. The template-smoke list now has 9 entries, equal to the 9 skills/*/SKILL.md files. The skill is public-safe (no task numbers or history) and its frontmatter matches the other skills (name, description, when-to-use). No issues found.

## Issues
None.
