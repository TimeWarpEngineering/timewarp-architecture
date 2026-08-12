---
title: TimeWarp.Architecture
description: Agent-readable home page for the TimeWarp.Architecture template host.
---

# TimeWarp.Architecture

A distributed .NET reference application and **project template** by
[TimeWarp Enterprises](https://timewarp.enterprises/). Clone it, generate an app
with `dotnet new timewarp-architecture`, and build on the same batteries-included
architecture the monorepo dogfoods.

This file is the **markdown twin** of the HTML home page (`/`). Agents may also
request `/` with `Accept: text/markdown` and receive this body (content
negotiation). Discovery index: [/llms.txt](/llms.txt). Auth story: [/auth.md](/auth.md).

## Built with

- .NET 10 and Blazor WebAssembly
- [FluentUI Blazor v5](https://github.com/microsoft/fluentui-blazor) + plain CSS design tokens
- [TimeWarp.State](https://timewarpengineering.github.io/timewarp-state/) (Redux-style state)
- [TimeWarp.Mediator](https://github.com/TimeWarpEngineering/timewarp-mediator) for CQRS and
  [FastEndpoints](https://fast-endpoints.com/) for APIs
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) orchestration + YARP ingress

## Agent-first surfaces

| Surface | Path |
|---------|------|
| Discovery index | [/llms.txt](/llms.txt) |
| Honest auth story | [/auth.md](/auth.md) |
| Crawl policy + Content Signals | [/robots.txt](/robots.txt) |
| Sitemap | [/sitemap.xml](/sitemap.xml) |
| OpenAPI | [/openapi/v1.json](/openapi/v1.json) |
| Scalar UI | [/scalar/v1](/scalar/v1) |
| Health | [/api/health](/api/health) |
| x402 voluntary tip (canonical) | [/api/tip](/api/tip) |
| x402 tip discovery alias | [/api](/api) |
| Metered capability (agent + pay) | [/api/demo/metered-capability](/api/demo/metered-capability) |
| MCP Server Card | [/.well-known/mcp/server-card.json](/.well-known/mcp/server-card.json) |
| Agent Skills index | [/.well-known/agent-skills/index.json](/.well-known/agent-skills/index.json) |
| A2A Agent Card | [/.well-known/agent-card.json](/.well-known/agent-card.json) |

Auth is **passkey / agent-key first** — not email/password. Free and discovery
routes never return HTTP 402. Unpaid tip/meter may return **402** only on those
paid paths when payment is enabled (see [/llms.txt](/llms.txt) Payment).

## Human demo UI (browser)

- Home (this page as HTML): [/](/)
- Sign in (passkey-first CTA): [/Login](/Login)
- Passkeys (technical ceremony demo): [/Passkeys](/Passkeys)

## Markdown access

```bash
# Twin URL (always markdown)
curl -sS https://<host>/index.md

# Same content via Accept negotiation on the HTML route
curl -sS -H 'Accept: text/markdown' https://<host>/
```

Content usage preferences: `ai-train=yes, search=yes, ai-input=yes` (see robots.txt).
