// Counter.ts — demonstrates dispatching a TimeWarp.State action from JavaScript.
// Exposed as window.Spa.Counter; the CounterPage.razor button's JavaScript onclick calls it
// directly (timewarp-state test-app shape) — no C# handler round-trips through IJSRuntime (TWA0026).
// Plain object (not a class) so the window.Spa namespace stays traversable by Blazor's
// string-identifier interop for any future caller.
// IncrementCountActionName must match CounterState.IncrementCounterActionSet.Action's
// assembly-qualified name (JsonRequestHandler resolves it with Type.GetType); counter-js-dispatch
// tests read this file and assert it.
import { timeWarpState } from "/_content/TimeWarp.State/js/timewarp-state.js";

export const Counter = {
  DispatchIncrementCountAction: () => {
    console.log("%cdispatchIncrementCountAction", "color: green");
    const IncrementCountActionName =
      "TimeWarp.Architecture.Features.Counters.CounterState+IncrementCounterActionSet+Action, Web.Spa";
    timeWarpState.DispatchRequest(IncrementCountActionName, { amount: 7 }).then();
  },
};
