# Review framework — task 126-004

**Date:** 2026-07-26
**Host task:** kanban/in-progress/126-004-remove-source-mode-package-symbols-from-template-always-package-mode-add-sourcename-literal-scan-to-template-smoke/
**Diff scope:** commit `70a45d80` on `dev` — feat(template): always package-mode; scan sourceName platform literals
**Plan / brief:** Drop foundationPackages/analyzerPackages/identityPackages; always exclude vendored platform trees; keep monorepo Use*Packages; add .cs-inclusive sourceName-literal smoke scan; prove a251980f gate.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator Phase 4b 2026-07-26

## Ground rules

- Reviewers are read-only on product code; write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues; zero issues is valid
- Re-verify falsifiable claims against the repo

## Key paths

- `.template.config/template.json`
- `timewarp-architecture.slnx`
- `tools/dev-cli/endpoints/template-smoke-command.cs`
- `AGENTS.md`, `HowToUpgradeToAnalyzerPackages.md`
- Dual-mode `Use*Packages` must remain in Directory.Build.props / consumers
