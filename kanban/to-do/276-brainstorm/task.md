# Brainstorm the SpaceXAI TypeScript SDK with configurable API keys, including music

## Description

This task is a brainstorm and design capture only. Nobody implements the SDK, a music client, or any other product code from this task. Turn the written recommendation into a later implementation task before any code is written.

Eric Zakariasson (@ericzakariasson), 2026-10-02, https://x.com/ericzakariasson/status/2106090512210088361

> experimental @SpaceXAI TypeScript SDK. npm install @xai-official/sdk. Text, voice, image, and video in one SDK, with the latest Grok models. Tools that run on their servers: real-time X search, web search, code execution, and remote MCP.

Steven asked on 2026-10-04 for a timewarp-architecture kanban task to brainstorm utilization of this SDK with configurable API keys, and to ask whether music and audio can be included too. Suno is a candidate, not a decision.

timewarp-architecture is a .NET distributed app template. `@xai-official/sdk` is TypeScript. Do not assume the package is vendored into the .NET projects. The question is utilization: which surface, if any, should call it, and why.

## Requirements

The brainstorm produces a written recommendation, not code. It covers all of the following:

1. Utilization. Say where this SDK could sit relative to the Architecture template versus staying a separate client. Consider an agent, the Blazor app, a small Node sidecar, or none, and say which (if any) and why. Do not assume a .NET binding or that the package is copied into the .NET projects.
2. Configurable API keys. No key baked into source or into committed config, and no single-vendor assumption. Keys are configuration the operator supplies. If music uses a second vendor, that key is separate configuration, not the same xAI key.
3. Modalities the SDK already claims: text, voice, image, and video, plus server tools (real-time X search, web search, code execution, and remote MCP). Compare those only to overlap that actually exists in this repo (see Notes). Do not invent further overlap.
4. Music and audio beyond the SDK's voice. Compare at least Suno against doing nothing. Say whether music belongs in the same configurable-provider story or stays out of the template. Audio storage, generation, and playback are questions for that recommendation, not requirements.
5. Leave these out of the recommendation's implementation scope, and do not do them under this task: implementing the SDK, calling Suno, storing real API keys, bank feeds, the financial app, and reverse-engineering any third-party API.

Acceptance for this task is the written brainstorm living with the task (in Notes, or a follow-up note on this task). It covers utilization, key configuration, and a yes, no, or later on music with the reason. Do not require a prototype.

## Checklist

- [ ] Written recommendation names the surface (agent, Blazor, a small Node sidecar, or none) and why
- [ ] Written recommendation keeps API keys as operator-supplied configuration, with a separate key if music is a second vendor
- [ ] Yes, no, or later on music, with the reason, including Suno versus doing nothing
- [ ] The recommendation lives on this task (Notes or a follow-up note), not in a prototype

## Notes

Checked in this repo on 2026-10-04. These are not model clients. Do not treat them as an existing AI provider:

- Ctrl+K is the in-app command palette. Roster, runner, ranker, and row are under `source/container-apps/web/projects/web-spa/features/application/command-palette/`. Palette state is under `source/container-apps/web/projects/web-spa/features/application/command-palette-state/`. It searches routes and commands. It does not call a model.
- The Blazor SPA references `TimeWarp.State` and `TimeWarp.State.Plus` in `source/container-apps/web/projects/web-spa/web-spa.csproj` and global-uses `TimeWarp.State`. That is the client store. It does not call a model.
- A search of `source/` and `documentation/` (excluding `obj/` and `bin/`) found no Grok, xAI, or OpenAI provider configuration. The only API-key constants found are `CDP_API_KEY_ID` and `CDP_API_KEY_SECRET` in `source/container-apps/web/features/tip/tip-environment-application.cs`. Those are tip-jar facilitator credentials, not a model key. Do not reuse that slot for this SDK.

Out of scope:

- Implementing `@xai-official/sdk`, or adding a Node project, sidecar, or .NET binding for it.
- Calling Suno or any other music or audio API.
- Storing real API keys in source, committed config, or this task.
- Bank feeds and the TimeWarp Financial app.
- Reverse-engineering any third-party API, including a bank or a model vendor that has not published one.

Acceptance criteria:

- A written brainstorm is on this task.
- It recommends a surface (agent, Blazor, a small Node sidecar, or none) and why, without assuming the TypeScript SDK is vendored into the .NET projects.
- It says how API keys stay operator-supplied configuration, and that a music vendor would get its own key.
- It answers yes, no, or later for music, with the reason.
- No prototype and no product code.

Created: 549436 (2026-10-04). Captured only. No product code.
