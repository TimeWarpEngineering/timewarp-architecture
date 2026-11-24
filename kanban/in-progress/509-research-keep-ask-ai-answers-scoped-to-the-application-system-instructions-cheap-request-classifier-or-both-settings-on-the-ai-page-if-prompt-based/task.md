# Research: keep Ask AI answers scoped to the application (system instructions, cheap request classifier, or both); settings on the AI page if prompt-based

## Description

Ask AI (Akili) answers anything. On 2026-11-24 it was asked "can you write me a hello world
file-based app in C# in one line?" and replied with `Console.WriteLine("Hello, World!");` and
`dotnet run hello.cs`. That is a general chatbot answer on a tenant's dime and has nothing to do
with the application. The assistant should help with this application: its pages, actions,
resources, and the person's own data, and decline or redirect everything else.

Today's instructions come from `AgentAskInstructions` in web-spa
(`features/application/agent/agent-ask-instructions.cs`), passed as `Instructions` on the chat
options and relayed through `xai-chat-mapping-server.cs`. Nothing in them restricts topic, and
nothing checks the request before it reaches the model. Site-level AI settings on the AI admin
page are `UpdateAssistantName` and `UpdateXaiByokMode` only.

This is a research task. Deliver `report/analysis.md` (`tw-analysis-report`) and a recommendation.
No product edits beyond the kitchen.

## Requirements

Compare at least these approaches, with a real measurement for each (same 30-prompt set: 15
in-scope asks drawn from the action catalog and pages, 15 off-topic asks including code help,
general knowledge, and prompt-injection attempts):

1. **Instructions only.** A scope section in `AgentAskInstructions` that names what the assistant
   does, what it declines, and the redirect copy. Measure refusal accuracy on grok-4.7 and the
   token cost it adds per turn.
2. **Cheap classifier before the model.** A small, fast request classifier that labels each ask
   in-scope / out-of-scope / unsafe before the main model sees it. Candidates to evaluate: a
   small xAI model with a strict JSON schema (`grok-4.1-fast` or the smallest current model
   listed on docs.x.ai), Cloudflare's AI Gateway guardrails or Firewall for AI if a Cloudflare
   path already exists in this deployment (`tw-deploy`), and a local embedding-similarity check
   against the action catalog descriptions (no second model call). Measure latency added,
   cost per request, and accuracy on the same set. Steve's phrase for this was a Cloudflare-style
   cheap evaluation that classifies the request.
3. **Both.** Classifier gates, instructions shape the answer for in-scope asks.

For each: where the check runs (web-spa before send, web-server relay, or the deploy edge), and
how a tenant admin turns it on or tunes it.

### Bring-your-own-key is exempt by default

Scope restriction exists to protect the site's spend. When the person is on their own xAI key
(BYOK), they pay for every token, so the default is no restriction at all. On a BYOK turn the app
applies none of it: no scope instructions, no classifier, no restriction. This is automatic from
the key mode, not a setting. The research measures nothing on BYOK beyond confirming the gate is
skipped there.

### Settings

If the recommendation includes a prompt or a toggle, specify the AI admin page change as a
follow-up task spec in `## Results`: a `ScopeInstructions` text field, a `ScopeMode`
enum (`Off`, `InstructionsOnly`, `ClassifierAndInstructions`) on site settings (no BYOK toggle: BYOK turns always bypass), with its own
`Update…` command like `UpdateAssistantName` (`tw-web-api-contracts`), surfaced on `AiPage`.
Note that 508 wires that page into prefill, so the new field gets a prefill tool for free.

### Evidence

- `report/prompt-set.md` with the 30 prompts and each approach's verdict per prompt.
- Latency and token numbers in a table in `report/analysis.md`.
- Screenshot of the current off-topic answer in `evidence/` for the before state.

## Goal (implementer restatement)

Ask AI answers anything on the site's key today. Find the cheapest way to keep it on this
application (its pages, actions, and the person's own data) and to turn everything else away,
then recommend one. Measure, do not guess: run one 30-prompt set (15 in-scope asks from the
action catalog and pages, 15 off-topic asks including code help, general knowledge, and
prompt injection) through the current instructions (before state), an instructions-only scope
section on grok-4.7, each cheap classifier candidate (a small xAI model with a strict JSON
schema, Cloudflare guardrails as a data point because no deploy target puts Cloudflare in
front, and a local embedding check against the catalog), and the combination. Record accuracy,
added latency, and added tokens or cost per turn, and say where each check runs and how an admin
turns it on.

Acceptance:
- `report/prompt-set.md` lists the 30 prompts and every approach's verdict per prompt.
- `report/analysis.md` (tw-analysis-report) has the latency/token table and a recommendation.
- `evidence/` holds the before-state off-topic answer.
- BYOK turns skip every part of the gate because of the key origin, with no setting; the
  report shows where that is decided.
- `## Results` holds the follow-up spec for the AI admin page (`ScopeMode`,
  `ScopeInstructions`, an `Update…` command per field) if the recommendation needs it.
- No product edits; the kitchen only.

## Checklist

- [x] Prompt set written; current behavior measured (before state)
- [x] Instructions-only variant measured
- [x] Classifier candidates measured (xAI small model, Cloudflare if applicable, local embedding)
- [x] Combined variant measured
- [x] BYOK turns bypass the gate automatically; no setting
- [x] `report/analysis.md` with recommendation and the AI-settings follow-up spec in `## Results`

## Session

- Created: 2026-11-24 from Steve's Ask AI test (hello-world answer). BYOK exemption added the
  same day: "when they're on bring your own key I don't care what they use it for, they're paying for it." Then: no override setting, skip it automatically on BYOK.
- 2026-11-25 implementer (task-work walk): measured all approaches live against xAI and Cloudflare
  Workers AI (`report/harness/`, raw data `report/data/`, about 1.10 USD of API spend). The app
  was not started; the before-state screenshot is rendered from the recorded relay reply.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 3d586669-33f4-4a8a-aff4-c7be0fee03fc, max-turns 500, Done — 2026-11-24T19:25:18Z

## Results

Deliverables: `report/analysis.md`, `report/prompt-set.md`, `report/harness/` (fixtures, measure,
embed, analyze), `report/data/*.json`, `evidence/before-hello-world.md` and `.png`.

| Approach (grok-4.7 main model) | Main set (60 runs) | Held-out (20) | Added latency, median | Added cost per turn |
|---|---|---|---|---|
| Before (current instructions) | 35/60, 5/30 off-topic declined | 5/10 (one run) | n/a | n/a |
| 1. Instructions only, scope v2 | 60/60 | 20/20 | ~0 ms | +182 tokens, 0.00009 USD cached |
| 2a. xAI grok-4.20 non-reasoning classifier | 60/60 | 16/20 | +743 ms | 0.00048 USD |
| 2b. Workers AI llama-3.1-8b-fast classifier | 54/60 | 16/20 | +433 ms | 0.00004 USD |
| 2c. Llama Guard 3 (AI Gateway guardrail model) | 32/60 | 5/10 | +349 ms | 0.00010 USD |
| 2d. Local embedding, bge-small (oracle threshold) | 27/30 | 8/10 | +28 ms | 0 |
| 3. 2a gates + scope v2 | 60/60 | 18/20 | +~860 ms on in-scope asks | +0.0005 USD in-scope; off-topic drops to 0.00033 from 0.0038 |

**Recommendation:** instructions-only scope, added by the web-server relay on `Host` and `Site`
key-origin turns only, with `ScopeMode` and `ScopeInstructions` on the AI page. No classifier now:
it was not more accurate, it adds about 0.75 s and about 11% cost to every legitimate turn, and it
saves money only above about 13% off-topic share of site-key asks. Do not use the guardrail model,
the embedding check, or a web-spa or edge check (details: `report/analysis.md` F3-F5).

BYOK: the relay is the only place that knows the key origin (`XaiApiKeyResolver` returns
`XaiApiKeyOrigin.User` for a personal key). A `User` turn gets no scope section and no classifier.
There is no setting for it. Today `XaiChatUpstream` drops the origin after resolution; the
follow-up carries it through.

### Follow-up task spec (not filed)

**Title:** Ask AI scope: the relay adds scope instructions on site-key turns; ScopeMode and
ScopeInstructions on the AI page

**Description.** Task 509 measured that a scope section in the instructions keeps Ask on this
app on `grok-4.7` (60/60, held-out 20/20) for about 182 cached prompt tokens per turn. Add it in
the web-server relay, where the key origin is known, and let a site admin turn it off or change
the text on the AI page. BYOK turns are never scoped.

**Requirements**

1. `TimeWarp.Identity.AssistantScopeMode` enum: `InstructionsOnly = 0`, `Off = 1`. The zero value
   is the protected one, so a migrated row or a body that omits the field stays scoped. Undefined
   values are rejected (same pattern as `XaiByokMode`). No BYOK value: BYOK turns always bypass.
2. `SiteSettings` gains `ScopeMode` and `ScopeInstructions` (trimmed; empty means the built-in
   default). `ScopeInstructionsRules` owns `MaxLength` (2,000), plain-text validity, and `Default`
   (the scope v2 text, `SCOPE` in `report/harness/fixtures.py` of task 509), shared with the
   contract validator the way `AssistantNameRules` is. `SetScopeMode` and `SetScopeInstructions`
   methods. Reseed does not rewrite either. EF migration: two columns, defaults `0` and `''`.
3. Contracts (`tw-web-api-contracts`), each a dedicated command like `UpdateAssistantName`:
   `UpdateAssistantScopeMode` (`PUT api/settings/assistant/scope-mode`,
   `[Operation("assistant-scope-mode.update", Group = "admin.ai")]`) and
   `UpdateAssistantScopeInstructions` (`PUT api/settings/assistant/scope-instructions`,
   `[Operation("assistant-scope-instructions.update", Group = "admin.ai")]`). `SettingsWrite`
   policy, `Version` concurrency token from `GetSiteSettings` (409 on mismatch), `ICommand`,
   `I…Details` plus validators, serialization round-trip tests. `GetSiteSettings.Response` carries
   `ScopeMode`, `ScopeInstructions`, and the default text so the page can show it.
4. Relay: carry `XaiApiKeyOrigin` with the key through `ResolvedKey` and `KeySlot` to
   `XaiChatUpstream.CompleteAsync`. When the origin is `Host` or `Site` and the mode is
   `InstructionsOnly`, put the effective scope text ahead of the client's `Instructions` in
   `ChatOptions.Instructions`, and refuse a client `system` role turn with 400 (web-spa never sends
   one). When the origin is `User`, send exactly what the client sent. The 8,000-character
   `MaxInstructionsLength` stays on the client text; the scope text is added after validation.
5. `AiPage`: an Answer scope card with the mode (Instructions only / Off) and a multiline text
   field showing the default when empty, with Reset to default. Each saves through its own
   `SiteSettingsState` action set listed in the page's `[Page(AgentActions = ...)]`, so task 508's
   prefill gives both fields a prefill tool.
6. Tests (Jaribu): Site origin sends instructions that start with the scope text; Host origin
   too; User origin sends the client text unchanged and is not refused for scope; `Off` sends the
   client text unchanged; a client `system` turn on a scoped turn returns 400; both commands
   reject a stale `Version` (409) and over-length text; contract round trips.
7. Proof: capture the instructions the relay sends on a Site turn and re-run task 509's harness
   (`measure.py scoped 2` and `PROMPT_SET=HELDOUT`) with that exact text. Expect 60/60 and 20/20,
   or record the difference in the task.

**Out of scope:** a request classifier (`ClassifierAndInstructions`; analysis F3/F4 and
recommendation 3 say when to add it), any BYOK toggle, web-spa-side or edge checks.

### How to validate

- **Smoke:** `python3 -I report/harness/analyze.py` from the task folder.
  **Expect:** the accuracy table, the latency and cost table, and the per-prompt tables match
  `report/analysis.md` F3/F4 and `report/prompt-set.md` row for row (they are generated from
  `report/data/*.json`).
- **Smoke (live, optional, about 1 USD, needs `XAI_API_KEY`):** `python3 -I report/harness/measure.py scoped 2`
  then `python3 -I report/harness/analyze.py accuracy`.
  **Expect:** scope v2 at or near 60/60, and Before far below it if `baseline` is re-run.
- **Smoke:** open `evidence/before-hello-world.png`.
  **Expect:** the hello-world prompt answered with `Console.WriteLine("Hello, World!");` and
  `dotnet run hello.cs`.

### Gates

- `./bin/dev build`: passed (build completed successfully; client-name scan passed).
- Test files: none added or edited. No production code changed (kitchen-only research), so no
  covering test files apply.
- `ganda repo audit`: first run failed on two checks outside this task: `kanban-landed` (task
  518's live kitchen differed from origin-home) and `vscode-window-icon` (local `.vscode/settings.json`
  missing; gitignored). Ran `ganda repo audit --fix --checks kanban-landed,vscode-window-icon`
  (boy-scout); no tracked file in this worktree changed. That run reported "Repository passes all
  audit checks."

