# How-to guides

Task-oriented guides for working in a TimeWarp Architecture solution.

## Platform and upgrade

- [How to release](how-to-release.md) — version SSOT, pins==version, post-publish template-publish-smoke gate
- [How to upgrade to analyzer NuGet packages](how-to-upgrade-to-analyzer-packages.md)
- [How to add your aggregate](how-to-add-your-aggregate.md) — domain → EF mapping → SaveChanges → tests
  ([ADR-0009](../conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md))
- [How to remove demo features](how-to-remove-demo-features.md)

## Authorization

- [How to swap the permission evaluator for an external PDP](how-to-swap-permission-evaluator-for-external-pdp.md)
  ([ADR-0010](../conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md))

## Local tooling

- [How to trust the ASP.NET dev certificate under WSL](how-to-trust-aspnet-dev-certificate-when-using-wsl.md)
- [How to prevent local commits to master](how-to-prevent-local-commits-to-master.md)
- [How to rename the default branch from main to master](how-to-rename-default-branch-from-main-to-master.md)
- [How to run Oakton commands](how-to-run-oakton-commands.md)
- [How to get all contents in a directory recursively](how-to-get-all-contents-in-directory-recursively.md)

## Ops / edge

- [How to configure Cloudflare as the agent-welcome outer ring](how-to-configure-cloudflare-edge-for-agent-welcome.md) — WAF/rate limits aligned with 104-015; do not block all AI bots; edge ≠ Identity/402

## Contracts and testing

- [Web API contracts](web-api-contracts/overview.md)
- [Testing guides](testing/) — endpoint tests, Jaribu co-located + suite migration (epic 145), host lanes
