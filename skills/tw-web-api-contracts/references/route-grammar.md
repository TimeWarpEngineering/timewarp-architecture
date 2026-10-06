# `[ApiRoute]` parameter constraint grammar

Tokens are `{Name}` or `{Name:constraint}`. A type/constraint starts **only** after a colon —
bare names such as `{Date}`, `{LocationId}`, `{ClientId}`, `{StaffId}`, and `{UserId}` keep the
full identifier and default to `string`. `{Name:string}` remains valid.

| Constraint token | Generated C# type |
|------------------|-------------------|
| *(omitted)* / `string` / `alpha` / `required` / `minlength(n)` / `maxlength(n)` / `length(n)` / `range…` / `regex…` | `string` |
| `guid` | `Guid` |
| `datetime` | `DateTime` (`GetRoute` formats `yyyy-MM-dd`) |
| `min(n)` / `max(n)` | `int` |
| any other token (`int`, `long`, `bool`, …) | the token as written |

Constraint arguments are the parenthesized-digits form only (`{RoleId:min(1)}`). Multiple
constraints, comma-separated args, catch-alls, and `{name=default}` are not parsed. `:string` is
stripped from the emitted `RouteTemplate`; other tokens are kept (`{RoleId:guid}`).
