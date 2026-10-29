# Explore generic OpenAI API support for Ask AI

## Description

Ask AI (`AgentAsk` / Ctrl-K Ask) and BYOK currently support only xAI (Grok) via `XAI:ApiKey` / `XAI:Model`, relayed through web-server so the key never reaches WASM. Explore adding generic OpenAI-compatible API support so operators and BYOK users can point Ask at other providers (OpenAI, Anthropic Claude via OpenAI-compatible endpoint, self-hosted gateways, etc.).

This is exploration only: produce a design / options analysis, not an implementation. Validate findings with a live test using the available Claude API key.

## Current state (starting points)

- `XAI:ApiKey` / `XAI:Model` (default `grok-4.7`): `readme.md`, `skills/tw-blazor/SKILL.md`, `skills/tw-deploy/SKILL.md`, `tools/dev-cli/services/xai-preflight.cs`.
- Server-side xAI relay + admin key status: `source/container-apps/web/features/agent-chat/` (`get-xai-api-key-status`, `xai-api-key-problems-application.cs`, admin AI endpoints).
- BYOK policy: `source/libraries/timewarp-identity/settings/site-settings.cs`, `xai-byok-mode.cs` (`Required` / `Allow` / `Off`).
- `Microsoft.Extensions.AI.OpenAI` 10.11.0 already pinned in `Directory.Packages.props` (note: do not float onto Responses API).

## Requirements

- [ ] Map the full xAI-only surface: config keys, DI registration (browser relay `IChatClient` vs server-side client), relay endpoint, Admin → AI UI, BYOK flows, `dev run` preflight, Playwright coverage (`ask-*-playwright-tests.cs`, `clear-xai-api-key-*`), docs/skills copy.
- [ ] Propose a generic provider config schema (e.g. `AI:Provider`, `AI:Endpoint/BaseUrl`, `AI:ApiKey`, `AI:Model`, or per-provider named configs). Address: backwards compat with existing `XAI:*`, secret storage (`secrets.protected_secrets`, DataProtection purpose `TimeWarp.Architecture.XaiApiKey`), host-wins vs admin-managed vs BYOK precedence, model allow-listing, per-user keys.
- [ ] Evaluate `Microsoft.Extensions.AI` + `Microsoft.Extensions.AI.OpenAI` for a provider-agnostic `IChatClient` (OpenAI-compatible `OpenAIClient` with custom endpoint). Confirm tool-calling / streaming / agent-loop (`ActionCatalogEntry.Execute`) still works against a non-xAI endpoint.
- [ ] Live-spike with the provided Claude API key: configure an OpenAI-compatible endpoint (Anthropic OpenAI-compat base URL or a minimal local proxy if needed — do not commit the key), send a chat completion through the same `IChatClient` path Ask would use, and record what works / what diverges (auth header, base URL, model id, tool schemas, streaming, error mapping). Never commit the key, never print it in logs.
- [ ] Report options (recommended vs alternatives) with scope, migration, and test plan: config + Admin UI + BYOK + preflight + Playwright + docs/skills updates. Note voice transcription (task 453, Grok Voice API) as explicitly out of scope unless trivially portable.

## Notes

- Operator has a Claude API key available for testing. Coordinate to get the key at spike time; use env var / user-secrets only.
- Anthropic's native Messages API is not OpenAI-compatible; use its OpenAI-compatible endpoint (or document why a second client package is needed and what that costs).
- Keep `XAI:*` working as-is until a migration is approved. No breaking change in this task.
- Related: task 453 (voice input, Grok Voice), tasks 447/444/446 (Ask turn behavior), `kanban/backlog/276-brainstorm` (multi-vendor key note).

## Results (required before done)

[Exploration outcome: provider surface map, recommended schema, spike results against Claude key, open questions]

### How to validate (required before done)

**Smoke** — read-only review plus spike log:

```bash
# from the task worktree
rg -l "XAI:ApiKey|XaiApiKey|xai-api-key" source tools documentation skills tests | sort
```

**Expect**:

- Design note names every xAI-only touchpoint and the proposed generic schema with precedence rules.
- Spike section records the Claude-key live test (endpoint, model, success/failure, divergences) without exposing the key.

**Automated gate**: none (exploration, no product code expected).
