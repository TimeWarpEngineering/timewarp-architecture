# 077: Web.Contracts compliance #1 — nullability ⟷ validator agreement (requests)

First slice of bringing `web-contracts` into compliance with the `web-api-contracts` skill, **one
decision at a time**. This task is **Decision 7** from the RFC
([[contract-conventions-rfc]] in `skills/web-api-contracts/analysis/`). Nothing else — mutability,
Response shape, class modifiers, the `[RouteMixin]` rename — is in scope here.

## Why

The type annotation is the contract; the validator must agree. Two disagreement patterns exist in
TWA request types today:

- **Forbidden — `= string.Empty` on a required field** (`+ NotEmpty()`). JSON omits the property →
  it stays `""` → `NotEmpty()` **passes** → silent wrong data. This is a real bug.
- **Discouraged — `string?` + unconditional `NotEmpty()`.** Functionally the validator rejects null,
  so nothing breaks at runtime, but the nullable annotation lies about intent and disarms the
  compiler's null analysis.

Both resolve the same way: if a field is required (has an unconditional `NotEmpty()`/`NotNull()`),
the type is **non-nullable `string` + `= null!`**; if it is genuinely optional, the type is
**`string?`** with **no** unconditional required rule. See `skills/web-api-contracts/references/nullability.md`.

(Copic has ~17 of these; it is **frozen/read-only** — a delivered client artifact, not a target.
It's the cautionary corpus this rule exists to prevent, not something we edit.)

## Scope — request types in `source/container-apps/web/web-contracts/features/**` only

| File | Problem | Fix |
|------|---------|-----|
| `hello/hello.cs` | `Query.Name` is `string?` + `NotEmpty()` | `public string Name { get; set; } = null!;` (keep `NotEmpty()`) |
| `analytics/track-event.cs` | `Command.EventName` is `string?` + `NotEmpty()` | `public string EventName { get; set; } = null!;` (keep `NotEmpty()`) |
| `todo-items/commands/create-todo-item.cs` | `Title` = `string.Empty` + `NotEmpty()`; `Note` = `string.Empty`, no rule | `Title` → `= null!` (keep `NotEmpty()`); `Note` → `string?` (drop the `= string.Empty` initializer) |
| `todo-items/commands/update-todo-item.cs` | same as create | same as create |

Notes:
- `hello` `Name` is a query-string filter with `NotEmpty()`, so it is treated as **required** here.
  If the intent is actually "optional filter," the correct fix is the opposite — keep `string?` and
  **drop** the `NotEmpty()`. Decide per intent; do not leave the contradiction.
- Only change the **type annotation + initializer + the validator rule that contradicts it**. Do not
  touch `{ get; init; }` vs `{ get; set; }` — that's the mutability decision (a later task).

## Explicitly OUT of scope (own future tasks / decisions)
- Mutability `init` → `set` on bindable props (Decision on mutability).
- `Response : BaseResponse` → ctor + `Guard` (Decision 5, Response shape).
- `sealed partial` → `static partial` shell (`todo-items` uses `sealed`).
- Empty query stubs `search-todo-items.cs`, `get-todo-item-by-id.cs` (finish-vs-delete — needs a call).
- `todo-item-dto.cs` `= string.Empty` — it's a DTO with no co-located validator; belongs to the
  Response/DTO-shape decision, not this one.
- `[RouteMixin]` → `[Route]` rename (053-002) — deliberately independent; this task does not touch it.

## Done when
- [ ] The 4 files above have no `string?` + unconditional `NotEmpty()` and no `= string.Empty` on a
      field that also has `NotEmpty()`.
- [ ] Repo-wide check is clean: no request type in `web-contracts/features/**` has a type/validator
      nullability disagreement (grep for `string?` near `NotEmpty` and `= string.Empty` near `NotEmpty`).
- [ ] `dev build` green (warnings-as-errors is enforced — a lingering `= null!` mismatch or unused
      using will fail the build).
- [ ] Diff is single-axis: only type annotations, initializers, and the contradicting validator
      lines changed. No mutability/shape/modifier churn.

## Notes
- This is compliance slice **#1 of N**. After it merges, the next task picks the next decision
  (candidates: Response shape / `BaseResponse` → ctor+Guard, mutability `init`→`set`, shell
  `static`, empty-stub disposition). One decision per task keeps each diff reviewable.
- Serialization tests are **not** required for this change (RFC Decision 3 leans "test only when a
  contract uses `required`/`init`/custom converters/non-default ctors"; these edits don't add any).
