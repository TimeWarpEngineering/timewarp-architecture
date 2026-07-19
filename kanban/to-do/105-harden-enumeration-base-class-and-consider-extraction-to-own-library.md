# Harden Enumeration base class and consider extraction to own library

## Description

`source/foundation/foundation-domain/enumeration/enumeration.cs` (Bogard-pattern enumeration
class) is serviceable but has several gaps worth closing before usage grows. Separately, evaluate
extracting it into its own first-party library (`timewarp-enumeration` or similar) so it can be
consumed outside the foundation packages.

## Requirements

Improvements identified in review (2026-07-19):

- **Cache `GetAll<T>` reflection** — currently every `Parse`/`FromValue`/`FromName` call
  re-enumerates fields via reflection. Add a static `ConcurrentDictionary<Type, T[]>` (or
  equivalent) cache.
- **Implement `IEquatable<Enumeration>` and `==`/`!=` operators** — today equality goes through
  the boxing `Equals(object)` path, and `member1 == member2` is reference equality even though
  semantic equality is Value + exact type. Real foot-gun.
- **Add generic `IComparable<Enumeration>`** — only non-generic `IComparable` exists.
- **`FromString` collision safety** — name and alternate-code lookup share one `FirstOrDefault`;
  if one member's name equals another member's alternate code the result is silently
  order-dependent. Detect/throw on ambiguity, or define precedence explicitly (name wins).
- **System.Text.Json converter** — no STJ support today; a subclass in a contract would serialize
  as `{"Value":..,"Name":..,"AlternateCodes":[..]}` and be unreconstructible on read (no public
  ctor, get-only props) — same silent-failure class as the PrincipalId bug fixed in 104-027.
  Converter should round-trip by Value (or Name) and fail closed on unknown values.
- **Analyzer for member declaration shape** — `GetAll` only sees `public static readonly` fields
  (`DeclaredOnly`); a member declared as a property or non-public silently vanishes from lookups.
  This is agreement-by-memory — per the standing prefer-analyzers directive, add a TWA diagnostic
  flagging Enumeration-subclass members not declared in the required shape.

## Checklist

- [ ] Cache GetAll reflection results
- [ ] IEquatable + operator ==/!= (Value + exact type semantics)
- [ ] IComparable\<Enumeration\>
- [ ] FromString ambiguity handling
- [ ] STJ JsonConverter (fail closed on unknown values) + round-trip tests
- [ ] Analyzer: enumeration members must be public static readonly fields
- [ ] Reconcile `#region Design` in enumeration.cs with the changes
- [ ] Decide: extract to standalone library (`timewarp-enumeration` or similar)?

## Notes

- **See https://github.com/ardalis/SmartEnum for ideas.** Ardalis created SmartEnum after Steve's
  enum class and a DevBetter meeting — he drew inspiration from Steve's implementation, so we can
  freely draw from his in return. Its feature surface (value converters, EF support,
  `TryFromName`/`TryFromValue`, comparison operators) is a useful checklist, same way
  StronglyTypedId was used as a spec for 104-027 rather than adopted.
- **Consider extracting to its own library** — `timewarp-enumeration` or similar — rather than
  keeping it inside `TimeWarp.Foundation.*`. A standalone package would let dependency-free
  libraries (e.g. timewarp-identity, should a behavior-carrying enumeration ever be needed there)
  consume it without dragging in the foundation stack.
- Context: review concluded the plain C# enums in `source/libraries/timewarp-identity`
  (TrustTier, PrincipalKind, CredentialType) should stay plain enums — they are pure
  discriminators and the identity library is deliberately runtime-dependency-free. This task is
  about the Enumeration class itself, not migrating identity.

## Session

- Created: 2026-07-19
