# Refactor dev-cli deploy and entra services and commands over 500 lines

## Parent

469

## Description

The dev-cli deploy and entra code is the densest cluster of over-limit files, and several of
them stack many top-level types. Split each file into one type per file, then look at the
remaining large classes for mixed responsibilities (argument parsing, process orchestration,
output rendering, and cloud API calls tend to sit together).

## Files (lines, top-level types)

- tools/dev-cli/services/template-smoke-harness.cs (1315, 3)
- tools/dev-cli/services/aspire-deploy.cs (919, 10)
- tools/dev-cli/services/deploy-operate.cs (814, 9)
- tools/dev-cli/endpoints/template-smoke-command.cs (662, 1)
- tools/dev-cli/endpoints/entra-setup-command.cs (662, 1)
- tools/dev-cli/services/entra-setup.cs (604, 1)
- tools/dev-cli/services/entra-tenants.cs (537, 4)
- tools/dev-cli/endpoints/deploy-command.cs (525, 1)

## Requirements

- One top-level type per file. Each file 500 lines or fewer.
- Endpoints stay thin. Logic that an endpoint file holds moves to a service.
- Behavior unchanged. tests/tools/dev-cli-tests still pass. `dev` AOT self-install still
  builds (see tw-dev-cli; the installed `bin/dev` goes stale after dev-cli edits).
- Wrap tools, do not reconstruct their internals, when moving process-calling code.

## Checklist

- [ ] Split multi-type files
- [ ] Decompose template-smoke-harness by responsibility
- [ ] Decompose aspire-deploy and deploy-operate by responsibility
- [ ] Trim endpoints
- [ ] dev build 0 warnings, dev-cli tests green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1a2d9-00bc-74e0-bf45-a6d476140429, max-turns 500, Blocked — 2026-11-03T19:18:22Z

## Blocked

2026-11-03: placeholder — work not finished
