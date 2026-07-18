# Mock Response Factory

Optional per endpoint — add `GetMockResponseFactory()` + SPA registration when SPA mock mode
needs the endpoint (the mock service falls back to the real API for unregistered request types).

Detect the repo's pattern before writing: contract-local `GetMockResponseFactory()` registered in
a `Dictionary<Type, Delegate>` (canonical) vs standalone `*MockFactory` classes in the SPA (found
in some solutions).

See the `tw-mock-response-factory` skill for implementation and SPA registration.
