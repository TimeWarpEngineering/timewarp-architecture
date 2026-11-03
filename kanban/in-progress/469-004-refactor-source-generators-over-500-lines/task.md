# Refactor source generators over 500 lines

## Parent

469

## Description

Four generators exceed 500 lines. A generator usually mixes syntax discovery, model building,
and text emission in one class. Separate those into collector, model, and emitter types per
generator, mirroring the pattern that works best among the four.

## Files

- source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs (866)
- source/foundation/foundation-contracts-generators/contracts-generator.cs (672)
- source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs (576)
- source/analyzers/timewarp-architecture-analyzers/generators/typed-id-source-generator.cs (548)

## Requirements

- Each file 500 lines or fewer. Generated output text is byte-identical.
- Generator tests under tests/analyzers pass unchanged. Golden under goldens/ingress unchanged
  (`dev regenerate-generated` produces no diff).
- Incremental generator pipeline shape preserved (no new non-cacheable steps).

## Checklist

- [ ] page-source-generator split into collector / model / emitter
- [ ] contracts-generator split
- [ ] fast-endpoint-source-generator split
- [ ] typed-id-source-generator split
- [ ] Generator tests and golden diff green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
