# Migrate web-server-integration-tests to Jaribu with C-create

## Description

The at-scale proof (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §7.3). ~24 files/~14 host-consuming classes. DEPENDS ON
145-002 (factory). Hybrid topology applies: slice-shaped tests CO-LOCATE into
source/container-apps/web/features/<slice>/ as `-tests.cs` runfiles (suite shrinks);
genuinely host-level BFF/cross-cutting tests stay suite-shaped, converted to Jaribu classes.

## Requirements

1. Triage every test class: slice-shaped → co-located runfile (grammar + preamble per
   tw-feature-placement); host-level → stays in suite, Fixie ctor-injection replaced by
   SetupOnce + HostGraphFactory Web+Api graph with the MockAccessTokenProvider override.
2. Suite csproj converts to Jaribu MTP (aggregator-style wiring + global.json pin mirror) or
   dissolves entirely if nothing host-level remains — triage decides; document the outcome.
3. web-jaribu-tests aggregator picks up new co-located files automatically (glob); bump
   template-smoke JaribuFamilyAggregators expected counts if exemplar files change
   (tw-feature-placement maintenance bullet).
4. WebServerTestConvention/Fixie plumbing for this suite deleted with the migration.
5. Record aggregate wall-clock before/after in Results — this is the data source for the
   145-008 gate.

## Checklist

- [ ] Triage table (class → co-locate | host-level) in task folder
- [ ] Co-located runfiles pass standalone + via aggregator
- [ ] Host-level remainder green under Jaribu via dev test
- [ ] Fixie plumbing for this suite removed; counts updated where needed
- [ ] Before/after wall-clock recorded; dev build 0/0; full dev test; template-smoke; kanban committed
