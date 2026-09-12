# documentation/ inventory (210-005)

76 markdown files plus 4 non-markdown companions. Disposition is one of
`delete` / `fold-into-skill:<name>` / `fold-into-Design-region:<file>`. Fold means extract
the still-true rule, then delete the page with the rest of the tree.

Flow-repo skills (`tw-csharp`, `tw-git`, `tw-kanban`, `tw-jaribu`, `tw-dev-cli`,
`tw-release`, `tw-agent-context-regions`) are not duplicated here. Pages whose surviving
rule already lives in those skills (or in `AGENTS.md`) are `delete`.

Non-markdown companions (`model.mdj`, two `.puml`, `ai-context.yaml`) have no enforcing
skill or code and delete with the tree.

## Files

| Path | Disposition |
|------|-------------|
| `developer/conceptual/api-design.md` | delete (contradicts endpoint-centric skill) |
| `developer/conceptual/architectural-decision-records/adr-template.md` | delete (ADRs retired as pages) |
| `developer/conceptual/architectural-decision-records/approved/0000-use-markdown-architectural-decision-records.md` | delete (no enforcing skill or code) |
| `developer/conceptual/architectural-decision-records/approved/0001-entity-class-region-naming-and-usage.md` | delete (UML association regions unused; Purpose/Design supersede) |
| `developer/conceptual/architectural-decision-records/approved/0002-assembly-identification-with-assembly-marker.md` | fold-into-Design-region:`Directory.Build.targets` |
| `developer/conceptual/architectural-decision-records/approved/0003-endpoint-centric-api-with-interface-based-validation.md` | fold-into-skill:tw-web-api-contracts |
| `developer/conceptual/architectural-decision-records/approved/0004-branch-naming-conventions.md` | delete (flow skill `tw-git`; AGENTS.md already points there) |
| `developer/conceptual/architectural-decision-records/approved/0005-git-merge-strategy.md` | delete (flow skill `tw-git`) |
| `developer/conceptual/architectural-decision-records/approved/0006-kanban-development-process.md` | delete (flow skill `tw-kanban`; numbering/casing stale) |
| `developer/conceptual/architectural-decision-records/approved/0007-http-endpoints-are-generated-fastendpoints-from-contracts-on-both-servers.md` | fold-into-skill:tw-web-api-contracts |
| `developer/conceptual/architectural-decision-records/approved/0008-feature-cohesive-folders-with-filename-grammar-layer-composition.md` | fold-into-skill:tw-feature-placement |
| `developer/conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md` | fold-into-skill:tw-aggregate-pattern |
| `developer/conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md` | fold-into-Design-region:`source/container-apps/web/platform/authorization/i-permission-evaluator-application.cs` |
| `developer/conceptual/architectural-decision-records/approved/overview.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0001-use-cc0-as-license.md` | delete (MADR examples) |
| `developer/conceptual/architectural-decision-records/examples/0002-do-not-use-numbers-in-headings.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0003-include-in-adr-tools.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0004-write-own-toc-tool.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0005-use-dashes-in-filenames.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0006-use-names-as-identifier.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0007-do-not-emphasize-line-headings.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0008-add-status-field.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0009-support-links-between-adrs-inside-an-adrs.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0010-support-categories.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0011-use-asterisk-as-list-marker.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/0012-use-curly-brackets-to-denote-placeholder.md` | delete |
| `developer/conceptual/architectural-decision-records/examples/overview.md` | delete |
| `developer/conceptual/architectural-decision-records/overview.md` | delete (M24 unedited MADR boilerplate) |
| `developer/conceptual/architectural-decision-records/project-structure-and-conventions/ai-context-test.md` | delete (M21 opposite of approved architecture) |
| `developer/conceptual/architectural-decision-records/project-structure-and-conventions/overview.md` | delete |
| `developer/conceptual/architectural-decision-records/project-structure-and-conventions/project-structure-and-conventions.md` | delete (M21) |
| `developer/conceptual/architectural-decision-records/proposed/overview.md` | delete (M27 stub) |
| `developer/conceptual/architectural-decision-records/proposed/xxx.md` | delete (M21) |
| `developer/conceptual/architectural-decision-records/proposed/xxxx-powershell-coding-standards.md` | delete (M27) |
| `developer/conceptual/component-naming-and-organization.md` | delete (M23 PascalCase tree + two-layout vs `tw-blazor-layout`) |
| `developer/conceptual/contribute.md` | delete |
| `developer/conceptual/features/application/is-processing.md` | delete (M22 orphaned; cited paths/class do not exist) |
| `developer/conceptual/features/overview.md` | delete (M27 stub) |
| `developer/conceptual/overview.md` | delete |
| `developer/conceptual/testing/end-to-end-testing.md` | delete (M27 stub) |
| `developer/conceptual/testing/integration-testing.md` | delete (superseded by `AGENTS.md` + `tw-feature-placement` / `tw-jaribu`) |
| `developer/conceptual/testing/overview.md` | delete (M27 stub) |
| `developer/how-to-guides/how-to-add-your-aggregate.md` | fold-into-skill:tw-aggregate-pattern |
| `developer/how-to-guides/how-to-agent-identity-host-split-web-vs-api.md` | fold-into-Design-region:`source/container-apps/api/platform/identity-host/agent-bearer-stores-module-infrastructure.cs` |
| `developer/how-to-guides/how-to-configure-cloudflare-edge-for-agent-welcome.md` | fold-into-Design-region:`source/container-apps/web/platform/abuse/abuse-rate-limit-options-application.cs` |
| `developer/how-to-guides/how-to-get-all-contents-in-directory-recursively.md` | delete |
| `developer/how-to-guides/how-to-prevent-local-commits-to-master.md` | delete (flow skill `tw-git`) |
| `developer/how-to-guides/how-to-progressive-profile-and-agent-human-link.md` | fold-into-Design-region:`source/container-apps/web/features/profile/profile-domain.cs` |
| `developer/how-to-guides/how-to-release.md` | delete (`AGENTS.md` version/pin policy + flow skill `tw-release`) |
| `developer/how-to-guides/how-to-remove-demo-features.md` | fold-into-skill:tw-slice-isolation |
| `developer/how-to-guides/how-to-rename-default-branch-from-main-to-master.md` | delete (one-time ops) |
| `developer/how-to-guides/how-to-run-oakton-commands.md` | delete (Oakton not the repo CLI; use `dev`) |
| `developer/how-to-guides/how-to-swap-permission-evaluator-for-external-pdp.md` | fold-into-Design-region:`source/container-apps/web/platform/authorization/i-permission-evaluator-application.cs` |
| `developer/how-to-guides/how-to-trust-aspnet-dev-certificate-when-using-wsl.md` | delete |
| `developer/how-to-guides/how-to-upgrade-to-analyzer-packages.md` | delete (greenfield package-mode already in `AGENTS.md` Platform packages) |
| `developer/how-to-guides/overview.md` | delete |
| `developer/how-to-guides/testing/how-to-add-lifecycles-to-tests.md` | delete (M20 stale `Setup`/`Cleanup`; superseded by `tw-jaribu`) |
| `developer/how-to-guides/testing/how-to-filter-tests-by-name.md` | delete (`AGENTS.md` Build/run/test + flow skill `tw-jaribu`) |
| `developer/how-to-guides/testing/how-to-filter-tests-by-tags.md` | delete (`AGENTS.md` Build/run/test + flow skill `tw-jaribu`) |
| `developer/how-to-guides/testing/how-to-write-endpoint-test.md` | delete (M27 stub) |
| `developer/how-to-guides/web-api-contracts/handling-mutability-in-api-contracts.md` | delete (already `tw-web-api-contracts` + `references/mutability.md`) |
| `developer/how-to-guides/web-api-contracts/handling-nullability-in-api-contracts.md` | delete (already `tw-web-api-contracts` + `references/nullability.md`) |
| `developer/how-to-guides/web-api-contracts/how-to-write-bff-api-contracts.md` | delete (stale `Commands/`/`Queries/` vs use-case folders) |
| `developer/how-to-guides/web-api-contracts/overview.md` | delete |
| `developer/overview.md` | delete (M27 stub) |
| `developer/reference/api-endpoint-source-generator.md` | fold-into-skill:tw-web-api-contracts |
| `developer/reference/dotnet-conventions.md` | delete (M25 stale net9.0; `AGENTS.md` Stack is net10) |
| `developer/reference/overview.md` | delete |
| `developer/standards/file-naming.md` | delete (flow skill `tw-csharp`; AGENTS.md Layout) |
| `developer/tutorials/overview.md` | delete (M27 stub) |
| `overview.md` | delete (M27 unedited boilerplate) |
| `release-notes.md` | delete (GitHub releases) |
| `roadmap.md` | delete (M27 stub) |
| `test-structure.md` | delete (`AGENTS.md` + `tw-feature-placement` / `tw-jaribu`) |
| `tools.md` | delete |
| `user/overview.md` | delete |

## Non-markdown (deleted with the tree)

| Path | Disposition |
|------|-------------|
| `developer/conceptual/architectural-decision-records/project-structure-and-conventions/ai-context.yaml` | delete |
| `developer/conceptual/model.mdj` | delete |
| `developer/reference/dependencies-with-nuget.puml` | delete |
| `developer/reference/dependencies.puml` | delete |

## Counts

- 76 markdown files inventoried
- fold-into-skill: 5 unique destinations (`tw-web-api-contracts`, `tw-feature-placement`, `tw-aggregate-pattern`, `tw-slice-isolation`) covering 7 files (0003, 0007, 0008, 0009, how-to-add-your-aggregate, how-to-remove-demo-features, api-endpoint-source-generator)
- fold-into-Design-region: 6 files (0002, 0010, agent-identity split, cloudflare edge, progressive profile, PDP swap)
- delete: remaining 63 markdown files (stubs, boilerplate, superseded, no enforcing skill/code, or already covered)
