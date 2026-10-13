# Referenceable resources (Ask `@` references)

A list or search query can be a resource type that people reference in Ask by typing `@`
(`@prefix:id`). Mark the outer contract class:

```csharp
[ApiEndpoint]
[EndpointAuthorize(Policy = PermissionIds.AdminFeedbackRead, AuthenticationSchemes = ...)]
[ReferenceableResource("feedback", "Feedback items",
  IdProperty = nameof(ListFeedback.Item.FeedbackItemId),
  LabelProperty = nameof(ListFeedback.Item.Title))]
public static partial class ListFeedback { ... }
```

The shape, which **TWA0033** enforces:

- `[ApiEndpoint]` + `[EndpointAuthorize(Policy = …)]`. The query runs as the signed-in user,
  and its `Policy` decides who sees the type in the `@` list. `Policy` is required: the `@`
  list does not read `Roles`, so a contract gated by `Roles` alone fails TWA0033.
- The nested `Query` is public, implements `IApiRequest`, has a public parameterless
  constructor, and has a public settable `string? Search`. No member other than `Search` may be
  `required`. Match case-insensitively, treat a blank term as "everything", carry it in
  `GetRouteWithQueryString()`, and cap its length in the `Validator`.
- An `IAuthApiRequest` query needs a public `UserId` setter. The `@` search stamps a fresh
  `Guid` there, not the user's id, so its handler must resolve the user from the authenticated
  principal and never read `UserId`.
- The nested `Response` is a public class or record class with an enumerable items property
  (`Items` by default; set `ItemsProperty` otherwise). The item type is public.
- Each item has `IdProperty` (a `[TypedId]` struct, or `Guid` when the slice's typed id lives
  in its domain project) and a string `LabelProperty`. A blank label shows the id. When no
  single field reads well, add a derived `[JsonIgnore]` display property.
- `Prefix` is lowercase letters, digits and `-`, starting with a letter, and unique (**TWA0034**).

Do not register the type anywhere. The SPA's `ResourceReferenceRegistry` is generated from the
marked contracts. The token names a resource and grants nothing: every action that later
receives the id is re-authorized by its own endpoint. If the contract has a
`GetMockResponseFactory()`, make it honour `Search` so mock mode filters too.
