# Lane triage — api-server-integration-tests (145-005)

| Class / file | Lane | Disposition |
|--------------|------|-------------|
| get-weather-forecasts-endpoint-tests | closed-box (Aspire) | **Deleted** — covered by co-located `get-weather-forecasts-tests.cs` HTTP tests |
| get-weather-forecasts-handler-tests | in-proc | **Merged** into co-located runfile as mediator Send test |
| get-weather-forecasts-request-validator-tests | in-proc (unit) | **Merged** into co-located host-free validator class |
| open-api-document-tests | closed-box | **Kept** suite-shaped Jaribu + Aspire SetupOnce |
| api-test-server-application-tests | n/a | **Deleted** — trivial Start_Without_Exception + forever skip |

**Assembly outcome:** remains `api-server-integration-tests` as **closed-box only** (OpenAPI).

**Round-2 correction:** the deleted `get-weather-forecasts-endpoint-tests` was Aspire-backed
(real separate process via `TestApiService` → `CreateHttpClient`), not in-proc as originally
labeled. Its port to the in-proc co-located lane is an INTENTIONAL trade per 143 findings
§4/§4b: only genuine process-isolation-dependent coverage (OpenAPI discovery pollution) stays
closed-box; ordinary endpoint behavior belongs to the fast in-proc lane. Closed-box HTTP
coverage of the weather endpoint no longer exists anywhere, by design.
