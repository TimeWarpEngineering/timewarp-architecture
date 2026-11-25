# `[ApiRoute]` route grammar

## Segments

Every `[ApiRoute]` template has this shape. Analyzer **TWA0051** checks it on every `[ApiRoute]`,
hosted or `[ClientOnlyContract]`, and a const route is checked by its value.

```text
route   = "api" 1*( "/" segment )
segment = kebab / token
kebab   = word *( "-" word )      ; word = 1*( a-z / 0-9 )
token   = "{" Name [ ":" constraint ] "}"   ; the whole segment
```

- The first segment is `api`. Every route is under `/api/`, including analytics and other
  fire-and-forget writes.
- No leading `/`. `GetRoute()` returns an app-relative path.
- Literal segments are lowercase kebab: `api/agent-links`, `api/users/current/profile`. Not
  `api/Feedback`, `api/TodoItems`, or `api/GetCurrentUser`.
- Separate words with a hyphen. The analyzer cannot see a missing word break
  (`api/weatherforecast` passes), so write `api/weather-forecasts`.
- A parameter token is a whole segment. `api/items/{ItemId:guid}` is valid; `api/item{ItemId}` is not.
- No empty segment, trailing `/`, or query string. Query values come from
  `IQueryStringRouteProvider`.
- Name the resource collection, not the verb: `api/users/current`, not `api/GetCurrentUser`. The
  HTTP verb and the `[Operation]` name carry the action.
- Admin-only resources go under `api/admin/`: `api/admin/roles`, `api/admin/feedback`. The same
  resource for the signed-in user goes under its own collection: `api/feedback`.

## Parameter constraints


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
