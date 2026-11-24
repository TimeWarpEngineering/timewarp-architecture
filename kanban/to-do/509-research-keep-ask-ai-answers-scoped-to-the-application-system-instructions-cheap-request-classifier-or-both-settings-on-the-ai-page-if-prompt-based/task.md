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

For each: where the check runs (web-spa before send, web-server relay, or the deploy edge), how
it interacts with BYOK mode (a user's own key may still be scoped by the site), and how a tenant
admin turns it on or tunes it.

### Settings

If the recommendation includes a prompt or a toggle, specify the AI admin page change as a
follow-up task spec in `## Results`: a `ScopeInstructions` text field and/or a `ScopeMode`
enum (`Off`, `InstructionsOnly`, `ClassifierAndInstructions`) on site settings, with its own
`Update…` command like `UpdateAssistantName` (`tw-web-api-contracts`), surfaced on `AiPage`.
Note that 508 wires that page into prefill, so the new field gets a prefill tool for free.

### Evidence

- `report/prompt-set.md` with the 30 prompts and each approach's verdict per prompt.
- Latency and token numbers in a table in `report/analysis.md`.
- Screenshot of the current off-topic answer in `evidence/` for the before state.

## Checklist

- [ ] Prompt set written; current behavior measured (before state)
- [ ] Instructions-only variant measured
- [ ] Classifier candidates measured (xAI small model, Cloudflare if applicable, local embedding)
- [ ] Combined variant measured
- [ ] `report/analysis.md` with recommendation and the AI-settings follow-up spec in `## Results`

## Session

- Created: 2026-11-24 from Steve's Ask AI test (hello-world answer).
