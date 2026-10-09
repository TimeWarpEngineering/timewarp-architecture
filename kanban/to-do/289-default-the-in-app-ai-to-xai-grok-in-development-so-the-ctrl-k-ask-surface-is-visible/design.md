# Default Ask to xAI Grok

Task 289. Supersedes task 271 design items 3 and 4 for the template default. The catalog mapping, approval, permissions, and WebMCP behavior from 271 stay as they are.

## Relay location

The model call lives on **web-server**, the BFF the WASM client already calls with the identity-session cookie. It does not live on api-server. api-server is a second hop the palette does not use, and a key there would still need this same authorized relay.

This is a thin HTTP relay, not AG-UI and not a server-side `FunctionInvokingChatClient`. Task 271's follow-up named AG-UI as the way to keep the key off the client. The browser already owns the agent loop, so a completion relay is enough. The server forwards messages, instructions, and tool declarations, and returns text plus function calls. It never executes catalog actions.

## Two clients

- The unkeyed `IChatClient` is `RelayChatClient`, registered by web-spa. It posts `CompleteAgentChat` through `IWebServerApiService`. It holds no key. `FunctionInvokingChatClient` wraps it, so tools still run in the browser store.
- web-server also composes `Web.Spa.Program.ConfigureServices` for prerender, so that unkeyed client exists on the server too. The key-bearing xAI client is therefore **not** the unkeyed `IChatClient`. The handler depends on `IAgentChatUpstream`.
- The upstream builds an OpenAI-compatible chat client (`GetChatClient(model).AsIChatClient()`, chat completions, not the Responses API) only when `XAI:ApiKey` is non-empty. Creation is lazy, so a missing key does not fail startup. The endpoint is `https://api.x.ai/v1` unless `XAI:Endpoint` is set.

## When the model is on

Any environment with a non-empty `XAI:ApiKey` is configured. Development does not invent a key. Outside Development the same rule holds: no key means the visible not-configured state, never a hidden Ask button and never a startup failure.

`XAI:UseFakeUpstream` is honored only when the host environment is Development or Testing. Production ignores it. When the flag is on in an allowed environment, the fake wins over a real key so tests stay deterministic. A developer who sets the flag in Development replaces Grok.

The fake, given no tool result, returns a function call named `page_context` (the same tool `PageAgentContext` registers). Given a tool result, it returns assistant text that includes that result. It never invokes a tool.

## Key

User-secrets id on web-server is `0e53fdd3-6f93-4d5a-9c86-040621f7929e`. The setup command is:

```pwsh
dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --project source/container-apps/web/projects/web-server/web-server.csproj
```

The value is not in the repo, not in any appsettings file, and not in the WASM payload. The status response carries `Configured`, `SetupCommand`, and `Model` only.

## Defaults

Checked https://docs.x.ai/docs/models on 2026-10-09. The recommended current chat and code model is `grok-4.7`. That id is an alias that tracks the latest stable Grok, so the template default does not pin a dated snapshot. Override with `XAI:Model`.

## Limits

`CompleteAgentChat` requires an authenticated principal (`identity-session` or `mock-identity-session`, policy `identity-session-authenticated`). Agent-token is not a scheme: an agent bearer must not spend the Grok key. The handler checks the principal again. Validators cap messages, text, tools, schema, instructions, and a summed character budget. Admission is two concurrent calls and 30 per minute per principal. Upstream failures are logged and returned as a generic problem so exception text cannot carry key material.

## Ask surface

`CatalogAgentAvailability` reads `AgentSurfaceState` (unknown / configured / not configured), not "is an `IChatClient` registered". The shell dispatches one configuration probe. Ctrl-K always shows Ask. Without a key the modal shows "AI not configured" and the pwsh command above.
