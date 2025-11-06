# End-to-End Playwright Tests

## Purpose
- Smoke-test our Playwright tooling by launching Chromium, loading a sample page, and capturing a screenshot.

## How to Run
- Execute `dotnet run --project Tests/EndToEnd.Playwright.Tests/EndToEnd.Playwright.Tests.csproj`.
- Override the headless setting by editing `Program.cs` if needed (default launches a visible browser with `SlowMo`).

## Output Location
- Screenshots write to `artifacts/EndToEnd.Playwright.Tests/<runStamp>/main-home.png` at the repo root.
- `runStamp` uses `CI_RUN_ID` when supplied; otherwise it falls back to the current UTC timestamp (`yyyyMMdd-HHmmss`).
- CI can collect outputs by globbing `artifacts/EndToEnd.Playwright.Tests/**`.

## Notes
- This harness is intentionally minimal; add new scenarios by branching from `Program.cs` or introducing a test runner when the suite grows.
