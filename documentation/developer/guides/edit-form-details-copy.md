# Edit-form working copies

An edit form binds a command. The state store keeps the fetched response. Those are two objects.
`From` builds the command when edit starts, and again on cancel. It does not run on every render.

## What the generator emits

The foundation contracts generator looks for an interface whose name is longer than `IDetails`,
starts with `I`, and ends with `Details`.

- A partial class named `Command` gets `static Command From(I*Details source)`. Every interface
  property is copied. `List<T>`, `HashSet<T>`, `Dictionary<TKey, TValue>`, rank-1 arrays, and the
  matching collection interfaces become new collections. A null nullable collection stays null. A
  null non-nullable collection becomes an empty one. Elements are shared, except a reference type
  other than `string` that implements `ICloneable`: each element is cloned. Other collection types
  are assigned as-is.
- A partial class named `Response` whose details interface has a real setter (not `init`) gets
  `ICloneable.Clone()`. The copy is `MemberwiseClone`, then those collections and cloneable
  references are replaced so the snapshot does not share them. Get-only and `[Computed]`
  auto-properties survive, including `GetProfile.Response.Avatar`. A get-only or `init` collection
  is written through `UnsafeAccessor` on `<Name>k__BackingField`. The response constructor, including
  Guard, does not run again.

Mark the command, the response, and every containing type `partial`. Do not write `Clone() => this`
on a mutable details response.

## Version stays off the interface

`Version` is a concurrency token, not a field the form edits, so `ISiteSettingsDetails` does not
include it. `From` finishes by calling this classic partial method (no access modifier, so a missing
body is erased):

```csharp
static partial void CopyBeyondDetails(Command command, ISiteSettingsDetails source)
```

The parameter names are `command` and `source`. `UpdateSiteSettings.Command` implements the hook and
copies `Version` from `GetSiteSettings.Response` or `UpdateSiteSettings.Response`. Route ids,
`EmailCopy`, and `AttachmentIds` are not copied unless a hook says so. An extra member that is
`required` cannot be set from the hook, because the hook runs after `new()`.

## How TimeWarp.State clones a contract

Pinned package: TimeWarp.State 12.0.0-beta.19. The task names 12.0.0-beta.17. Both generators call
`ICloneable.Clone()` when the nested object implements that interface, and otherwise construct a new
instance and copy fields, including auto-property backing fields, through `UnsafeAccessor`. An
identity clone shares the instance. That is safe only for an immutable contract. A mutable details
response therefore implements a real `Clone`.

## Diagnostics

| Id | When |
| --- | --- |
| TWA0047 | A mutable `I*Details` response has `Clone` that returns `this`. |
| TWA0048 | That command or response, or a containing type, is not `partial`. |
| TWA0049 | A partial command has no parameterless instance constructor, so `From` is not generated. |
