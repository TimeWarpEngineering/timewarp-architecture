# Enumeration Class Tests And Code Review Fixes

## Description

Add comprehensive tests for `Common.Domain/Enumeration/Enumeration.cs` and address all issues found during code review.

## Checklist

### Phase 1: Create Tests (before any changes)

- [ ] Create test project/file for Enumeration class
- [ ] Test `GetAll<T>()` returns all static fields
- [ ] Test `FromValue<T>()` with valid and invalid values
- [ ] Test `FromName<T>()` with valid and invalid names
- [ ] Test `FromAlternateCode<T>()` with valid and invalid codes
- [ ] Test `FromString<T>()` with name, alternate code, and invalid input
- [ ] Test `CompareTo` with equal, less, greater, null, and non-Enumeration types
- [ ] Test `Equals` and `GetHashCode` (same value, different value, different type, null)
- [ ] Test `ToString` returns Name
- [ ] Test constructor sets properties correctly
- [ ] Test `AlternateCodes` defaults to empty list when null passed

### Phase 2: Address Code Review Issues

- [ ] Fix `Parse` return type: change from `T?` to `T` (it always throws on miss, never returns null)
- [ ] Update all `From*` methods return types from `T?` to `T` to match
- [ ] Replace bare `Exception` in `Parse` with `InvalidOperationException`
- [ ] Fix `FromString` to match its XML doc (either add `int.TryParse` for value lookup or correct the doc)
- [ ] Add type guard in `CompareTo` to handle non-Enumeration types gracefully
- [ ] Remove redundant null-conditional in `Equals` (line 100: `value?.GetType()` -> `value.GetType()`)
- [ ] Replace generic type parameter `K` in `Parse` with `object` (K is unused beyond ToString)
- [ ] Change `AlternateCodes` from `List<string>` to `IReadOnlyList<string>` to prevent mutation
- [ ] Remove commented-out default constructor (line 10)
- [ ] Fix XML doc typos: "form" -> "from", "from  its" -> "from its", "from is" -> "from its"

### Phase 3: Verify and Review

- [ ] Run all tests to confirm they pass after changes
- [ ] Request AI code review of the updated Enumeration class

## Notes

- File: `TimeWarp.Architecture/Source/Common/Common.Domain/Enumeration/Enumeration.cs`
- No existing tests found for this class
- Uses Fixie test framework (not xUnit/MSTest)

## Closeout (2026-06-27 — done)
Class relocated to `source/foundation/foundation-domain/enumeration/enumeration.cs` (ns
`TimeWarp.Architecture.Enumerations`). All Phase 1 + Phase 2 items completed:

**Tests (Phase 1):** new `tests/foundation/foundation-domain-tests/` project (added to the slnx) with
a `Color` test enumeration and **21 passing** cases covering GetAll, FromValue/FromName/
FromAlternateCode/FromString (valid + invalid), CompareTo (ordering / null / non-Enumeration),
Equals + GetHashCode, ToString, the constructor, and the null-alternateCodes default.

**Code-review fixes (Phase 2):**
- `Parse` and all `From*` now return `T` (not `T?`) — they always return or throw.
- Bare `Exception` → `InvalidOperationException`.
- `CompareTo` now guards non-Enumeration input (`ArgumentException`) and null (sorts first) instead of
  an `InvalidCastException` from a blind cast.
- Removed the redundant `value?.GetType()` null-conditional in `Equals`.
- Generic `Parse<T, K>` → `Parse<T>(object value, …)` (K was unused beyond ToString).
- `AlternateCodes` `List<string>` → `IReadOnlyList<string>` (prevents external mutation).
- Removed the commented-out default constructor.
- `FromString` XML doc corrected to match behavior (name or alternate code; no value lookup).
- Fixed the doc typos ("form"→"from", double space, "from is value"→"from its value").

`dev build` green; the one subclass caller (`foundation-server/cors-policy`) still compiles.

## Jaribu parallel (2026-06-27)
Added a Jaribu DUPLICATE of these tests (not a replacement) as a small worked example of both
frameworks against the same class: `tests/foundation/foundation-domain-jaribu-tests/enumeration.cs`
(a standalone .NET 10 runfile — `dotnet run tests/foundation/foundation-domain-jaribu-tests/enumeration.cs`).
Same 21 cases, **21 passed**. Added `TimeWarp.Jaribu` to root CPM. The Fixie suite
(`tests/foundation/foundation-domain-tests`, in the solution) remains the primary; the Jaribu runfile
demonstrates the runfile-based approach side-by-side.
