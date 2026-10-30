# Built-with lists TimeWarp DevTools and links YARP

## Description

The Home page "Built with" card still names Redux DevTools, which was removed
in TimeWarp.State 12.0.0-beta.11 (see base-component.cs Design note; the AppHost
now wires TimeWarp.State DevTools, task 335-001). Name TimeWarp DevTools
instead, and link YARP to https://github.com/dotnet/yarp. Apply to both the
Blazor card (HomePage.razor) and its markdown twin (wwwroot/index.md) so they
stay in agreement.

Reported 2026-10-28 with screenshot.

## Checklist

- [x] HomePage.razor: "with Redux DevTools" becomes "with TimeWarp DevTools";
  YARP becomes a NavLink to https://github.com/dotnet/yarp.
- [x] wwwroot/index.md twin: same two updates in markdown form.
- [ ] Verify: grep shows no remaining "Redux DevTools" in web-spa UI surfaces.

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- Implementation: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a18aef-71f6-7701-b5cf-d7a4321ec135, max-turns 500, Blocked — 2026-10-30T03:52:05Z

## Notes

No twin-consistency test covers the list text (only "Built with" heading in
catalog-agent-tests.cs), so both files are edited by hand.

## Blocked

2026-10-30: Work was not started: the previous turn returned Done without a commit. Unblock by letting this walk run `ganda context --json`, finish the Redux DevTools verification, then commit and push on task 451.
