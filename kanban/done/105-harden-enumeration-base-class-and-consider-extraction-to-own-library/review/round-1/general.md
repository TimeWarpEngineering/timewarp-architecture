# Round 1 — general
**Date:** 2026-10-04
**Scope reviewed:** full branch diff vs origin/master (enumeration.cs, JSON converter + factory,
ContractSerializationDefaults wiring, foundation-contracts → foundation-domain reference, TWA0028
analyzer + registrations, tests)

## Summary

The change replaces per-call reflection with a per-subclass `MemberCache<T>`. It validates
ambiguity once and fails closed, adds semantic `==`/`IEquatable`, the full comparison set, and a
name-based STJ converter registered on the contract seam. It also adds TWA0028 for member shape.
The implementation matches the task requirements and the reconciled Design regions. I re-ran the
gates on this worktree: `dev build` 0/0, foundation-domain-tests 62/62, foundation-contracts-tests
25/25, analyzers 192/192, and `ganda repo audit` passes. The `==` audit claim also holds: the only
production subclass is `CorsPolicy`, and nothing compares it by reference. No bugs found. The two
nits below cover edge semantics.

## Issues

### Issue 1 — Severity: nit
- File: source/foundation/foundation-domain/enumeration/enumeration.cs:278
- Description: when a public static field typed as T is *permanently* null (e.g. a placeholder
  `public static readonly Foo? None = null;`), `complete` never becomes true. Every lookup then
  re-reflects and rebuilds the indexes. The results stay correct, but the cache is silently lost.
- Suggestion: accept as is, or cache once the type initializer has finished (e.g. check
  `RuntimeHelpers.RunClassConstructor` first, then treat remaining nulls as absent).
- Status: open

### Issue 2 — Severity: nit
- File: source/foundation/foundation-domain/enumeration/enumeration.cs:146
- Description: `CompareTo` orders by Value only, but `Equals` requires the same runtime type. Two
  members of different subclasses with the same Value therefore compare as 0 while
  `Equals`/`==` is false. That is the CompareTo/Equals inconsistency that CA1036 guidance warns about.
- Suggestion: document it (the Design region already says "Comparison is by Value only"), or
  throw/order by type name across subclasses.
- Status: open
