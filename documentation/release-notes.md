---
uid: TimeWarp.Architecture:ReleaseNotes
title: TimeWarp.Architecture Release Notes
---

## 2.0.0-beta.17

### TimeWarp.Foundation.Contracts

- **Fix (task 053-003):** `[ApiRoute]` parameter names that end in type-like letters (`Date`,
  `LocationId`, `ClientId`, `StaffId`, `UserId`, …) are parsed as the full identifier. A type
  constraint is recognized only after an explicit colon (`{Name:guid}`). Bare `{Name}` defaults to
  `string`. `{Name:string}` remains valid. Consumers on Foundation.Contracts 2.0.0-beta.5 through
  2.0.0-beta.16 should upgrade to 2.0.0-beta.17 (or later) and can drop the `{Name:string}`
  workaround on string route params.
- **Change (task 053-004):** `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` are
  public types in `TimeWarp.Foundation.Features` (post-initialization output), not internal types
  in the consumer RootNamespace. FastEndpoint and ingress matching uses that FQN. Contracts that
  already `global using TimeWarp.Foundation.Features` need no source change.
