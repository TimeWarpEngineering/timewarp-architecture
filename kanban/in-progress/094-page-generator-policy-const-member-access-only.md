# Page generator Policy: one pit of success (const member access only)

## Description

`PageSourceGenerator` mishandles `[Page(..., Policy = …)]` and encodes a bad model.

### Bugs / design defects today

1. **Silent wrong auth** — only string **literals** are read for `Policy`. Any other
   expression is skipped → emit falls back to `Policies.Anonymous`. Compile succeeds;
   nav/`AuthorizeView` is wrong.
2. **Identifier glue** — emit is `Policies.{attributeString}` treating the attribute
   value as a C# **member name**, not as the registered policy **string**. That cannot
   honestly carry claim values like `"settings.edit"` and punishes const-field binding.
3. **Multiple weak authoring shapes** — literals, `nameof`, hoped-for member access —
   more ways in = more footguns. **Pit of success: one way.**

### Required model (beta — break freely)

`Policy` on `[Page]` is a **const string expression** that *is* the registered policy
name (product-owned constants). Generator **preserves that expression** on the generated
static property. No `Policies.` prefix glue. No string literals. No `nameof` alternate path.

**Only allowed authoring forms:**

```csharp
// Policy required for gated pages — bind to product const (any value: nameof(X) or "settings.edit")
[Page("/settings", Policy = Policies.SettingsEdit)]

// Omitted — Anonymous only (product must define Policies.Anonymous)
[Page("/dashboard")]
```

**Product constants (examples):**

```csharp
public static class Policies
{
  public const string Anonymous = nameof(Anonymous);              // template style
  public const string SettingsEdit = "settings.edit";             // claim-style value
  public const string CanViewAdminPage = nameof(CanViewAdminPage);
}
```

**Emit:**

```csharp
// Policy = Policies.SettingsEdit  →
public static string Policy { get; } = Policies.SettingsEdit;

// Policy omitted →
public static string Policy { get; } = Policies.Anonymous;
```

`INavigableComponent.Policy` remains `string` (ASP.NET / `AuthorizeView` seam). Strength is
**compile-time binding to a product const**, not a custom attribute type or platform enum
(open-ended product policies; enum in Generators package is the wrong layer).

### Reject with diagnostic (never silent Anonymous when Policy was written)

| Argument | Result |
|----------|--------|
| omitted | emit `Policies.Anonymous` |
| `Policies.SettingsEdit` (const member access / equivalent const ref) | emit that expression |
| `Policy = "…"` string literal | **diagnostic** — use a const field |
| `Policy = nameof(...)` | **diagnostic** — use `Policies.X` (one way) |
| other / unparseable | **diagnostic** |

Beta: no compat shims. Downstream (Crunchit) drops `Policy = "SettingsEdit"` and uses
`Policy = Policies.SettingsEdit`.

## Checklist

- [ ] Replace Policy parse/emit: expression passthrough for const member access; no
      `Policies.` + string glue
- [ ] Default omit → `Policies.Anonymous` only when `Policy` argument absent
- [ ] Diagnostic when `Policy =` is present but is a string literal
- [ ] Diagnostic when `Policy =` is `nameof(...)` (disallowed — force const member access)
- [ ] Diagnostic when `Policy =` is any other unsupported shape (never fall back to Anonymous)
- [ ] Generator Design region documents the single authoring form and why
- [ ] Unit tests: member access with value ≠ identifier (`"settings.edit"`); omit → Anonymous;
      literal rejected; nameof rejected; garbage rejected
- [ ] Update residual page docs / Purpose on generator (not AGENTS table sprawl)
- [ ] Template: confirm `Policies.Anonymous` exists; optional dogfood one non-Anonymous
      `[Page(..., Policy = Policies.…)]` if a real gated page exists
- [ ] Bump Generators package version for publish after merge (release ops)

## Notes

### Why not nameof / literals / “keep for now”

- **One way that works for all products** beats ten ways that work for the template only.
- `nameof(Policies.SettingsEdit)` is `"SettingsEdit"`, which is **wrong** when the const
  value is `"settings.edit"`. Teaching nameof as preferred would encode a second footgun.
- String literals are untyped noise and were the weak path that forced identifier glue.
- We are in **beta** — break the API; do not half-migrate.

### Why not enum / custom strong type on the attribute

- Policies are product-open; Generators must not own a closed enum of policy names.
- Custom structs are not valid attribute arguments in general.
- Product `const string` fields are the idiomatic strong binding for this stack.

### Out of scope

- Runtime `AddPolicy` / claim issuance (product).
- Changing `INavigableComponent` away from `string` (framework seam).
- Crunchit product PR (consumes fixed Generators afterward).

### Related

- Source: `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs`
- Package: `TimeWarp.Architecture.Generators`
- Template `Policies`: `web-spa/features/authorization/authorization-constants.cs`
- Tests: `tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs`
- Downstream discovery: Crunchit Settings / generators cutover (literal workaround)



### Implementation plan (2026-07-15)

1. Rewrite Policy parse in `PageSourceGenerator.GetPage`:
   - Route: string literal only (unchanged)
   - Policy omitted → emit expression `Policies.Anonymous`
   - Policy = MemberAccess / IdentifierName → emit expression text as-is
   - Policy = string literal / nameof / other → Diagnostic TWE005, do not emit page partial
2. Emit: `public static string Policy { get; } = {expression};` — no glue
3. Design + Purpose regions; AnalyzerReleases.Unshipped TWE005
4. Expand page-source-generator-tests for happy path + diagnostics
5. Confirm template Policies.Anonymous exists (no SPA call sites use Policy= yet)
6. Version bump to 2.0.0-beta.4 (single repo version) for next publish


## Session

- Created: from Crunchit generators cutover (nameof/literal footgun)
- Revised: 2026-07-15 — pit of success: const member access only; break beta glue model
