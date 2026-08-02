# Lane triage — api-server-integration-tests (145-005)

| Class / file | Lane | Disposition |
|--------------|------|-------------|
| get-weather-forecasts-endpoint-tests | in-proc | **Deleted** — covered by co-located `get-weather-forecasts-tests.cs` HTTP tests |
| get-weather-forecasts-handler-tests | in-proc | **Merged** into co-located runfile as mediator Send test |
| get-weather-forecasts-request-validator-tests | in-proc (unit) | **Merged** into co-located host-free validator class |
| open-api-document-tests | closed-box | **Kept** suite-shaped Jaribu + Aspire SetupOnce |
| api-test-server-application-tests | n/a | **Deleted** — trivial Start_Without_Exception + forever skip |

**Assembly outcome:** remains `api-server-integration-tests` as **closed-box only** (OpenAPI).
