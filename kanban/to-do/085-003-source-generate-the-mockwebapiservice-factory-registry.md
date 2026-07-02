# Source-generate the MockWebApiService factory registry

## Parent
085-analyzer-and-source-gen-opportunities-to-remove-inference-collected-candidates

## Description

`MockWebApiService.Factories` is a hand-maintained `Dictionary<Type, Delegate>`; a contract can
define `GetMockResponseFactory()` and never be registered (task 078 had to hand-add the CreateRole
entry). Source-generate the registry: scan referenced contract types for the
`GetMockResponseFactory()` convention and emit the dictionary population into a partial of
`MockWebApiService` — the manual registration step disappears entirely.

## Checklist

- [ ] Generator (new or existing web-spa-facing generator assembly — NOT the analyzer-only
      convention assembly) emitting the registry partial from referenced contract metadata.
- [ ] `MockWebApiService` becomes partial; hand dictionary replaced by generated member.
- [ ] Generator tests in the sourcegen test project (contract with factory → registered; without →
      absent).
- [ ] Verify SPA mock mode still works (`MOCK_WEB_API` build) + integration tests green.

## Notes

- Convention detection: `public static MockResponseFactory<T> GetMockResponseFactory()` on the
  static contract shell. Cross-assembly metadata scan, like the FastEndpoint generator does.
