# Generate clone and From(I*Details) copy for edit contracts

## Description

Source generator (or analyzer-backed generator) driven by `I*Details` interfaces:

- For every Command implementing `I*Details`: `static Command From(I*Details source)` copying
  every interface member. Lists copied, not shared (`List<T>` on editable details).
- For every mutable Response implementing `I*Details` (setters): a real deep `Clone()` so
  TimeWarp.State's per-action state clone does not share a mutable instance. Get-only /
  `[Computed]` members (e.g. `GetProfile.Response.Avatar`) must survive the clone.
- Members outside the interface that the Command needs (e.g. `Version`) — decide: widen the
  interface, or a generator hook / partial method. Record the decision.

Parent: see parent kitchen for the pattern.

## Checklist

- [ ] Verify how TimeWarp.State 12.0.0-beta.17's generated state cloner treats contract
      objects (ICloneable? field copy?) and get-only members
- [ ] Generator + diagnostics (e.g. mutable `I*Details` Response with `ICloneable => this`)
- [ ] Unit tests for generated `From` and `Clone`
- [ ] Short developer doc for the edit-form pattern under `documentation/developer/`

## Results

## Parent

- 489
