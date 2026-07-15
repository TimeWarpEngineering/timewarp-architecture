# Page generator Policy should accept nameof not only string literals

## Description

`PageSourceGenerator` only reads `Policy` when the attribute argument is a **string
literal**. Consumers that write the idiomatic, refactor-safe form:

```csharp
[Page("/settings", Policy = nameof(Policies.SettingsEdit))]
```

get **silent fallback to `Policies.Anonymous`** because `nameof(...)` is an
`InvocationExpressionSyntax` (not `LiteralExpressionSyntax`) and is skipped.

That is a footgun: compile succeeds, nav/auth policy is wrong.

### Evidence

`page-source-generator.cs` argument walk (approx.):

```csharp
if (arg.Expression is not LiteralExpressionSyntax lit || lit.Token.Value is not string value)
  continue;

if (arg.NameEquals?.Name.Identifier.Text == "Policy")
  policy = value;
```

Emitted policy:

```csharp
public static string Policy { get; } = Policies.{policy ?? "Anonymous"};
```

Discovered while building Crunchit portal (SPA generators cutover + Settings
`Policies.SettingsEdit`). Product workaround today: `Policy = "SettingsEdit"`
(string literal matching the const **identifier**, not the claim value).

## Checklist

- [ ] Accept `nameof(Identifier)` and `nameof(Type.Member)` for `Policy` (extract
      simple name of the last member → e.g. `SettingsEdit`)
- [ ] Optionally accept `nameof(Policies.SettingsEdit)` when `Policies` is the
      containing type — still emit `Policies.SettingsEdit`
- [ ] Keep string-literal support for existing call sites
- [ ] Reject / diagnose unsupported expression shapes (const field refs like
      `Policies.SettingsEdit` without nameof are harder — decide: support
      simple member access if it resolves to a const string of an **identifier
      name**, or document nameof-only)
- [ ] Generator unit/integration test: `[Page("/x", Policy = nameof(Policies.Foo))]`
      emits `Policies.Foo`, not `Anonymous`
- [ ] Docs / AGENTS: preferred form is `Policy = nameof(Policies.X)`
- [ ] Pack/publish Generators when fixed so downstream (Crunchit) can drop literal workaround

## Notes

### Preferred behavior

| Attribute argument | Emitted property |
|--------------------|------------------|
| `Policy = "SettingsEdit"` | `Policies.SettingsEdit` (existing) |
| `Policy = nameof(Policies.SettingsEdit)` | `Policies.SettingsEdit` (new) |
| omitted | `Policies.Anonymous` (existing) |

### Out of scope

- Changing claim **values** (`"settings.edit"`) — Policy attribute still names the
  **C# identifier** under `Policies`, not the capability string value.
- Runtime policy registration (product `AddPolicy`).

### Related

- Generator package: `TimeWarp.Architecture.Generators`
- Source: `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs`
- Downstream: Crunchit task 033-007 architecture gaps; Settings page uses literal workaround post 031-003
