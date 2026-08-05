# Review Round 1 — merged findings

Reviewer: general-purpose (sonnet), read-only. Scope per review-framework.md.

| # | Severity | File | Finding | Status |
|---|----------|------|---------|--------|
| F1 | major | source/container-apps/web/platform/postgres/postgres-db-module-server.cs:21 | Design region still claimed migrations apply "before web-server starts" — task 155 removed the wait edge; ADR amendment and program.cs say the opposite | **fixed** (`eb9648fe`) — reworded to no-wait-edge truth with the fresh-volume caveat |
| F2 | major | scripts/postgres/ef-shared-variables.ps1:7-9 (+ overview.md canonical CLI) | Scripts used web-infrastructure as EF startup project with the FALSE claim "web-server does not reference Design" (the same commit added that very reference); contradicted how-to §8 and actual AppHost wiring while claiming parity | **fixed** (`eb9648fe`) — startup project standardized on web-server everywhere; comment corrected |
| F3 | nit | ADR-0009 line ~74 | Original Decision Outcome bullet still reads WaitForCompletion with no pointer to the amendment | **fixed** (`eb9648fe`) — inline superseded-pointer added |

Clean areas verified by reviewer: migration/snapshot/entity-config consistency (schemas,
TypedIds, concurrency tokens, indexes — no drift), design-time factory resolution order,
membership-targets + .editorconfig carve-out scoping (no over-exclusion), hosted-service
removal completeness, dotnet-ef/EF/Design/Tools pin coherence (preview Aspire EF pin is a
documented accepted risk), how-to/how-to-remove/file-naming accuracy, zero code EnsureCreated
references.
