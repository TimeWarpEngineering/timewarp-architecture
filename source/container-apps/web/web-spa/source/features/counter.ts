// Counter.ts — demonstrates dispatching a TimeWarp.State action from JavaScript.
// Exposed as window.Spa.Counter; called by features/counter/pages/CounterPage.razor.
// Plain object (not a class) so Blazor's string-identifier JS interop can traverse the path.
import { timeWarpState } from "/_content/TimeWarp.State/js/TimeWarpState.js";

export const Counter = {
  DispatchIncrementCountAction: () => {
    console.log("%cdispatchIncrementCountAction", "color: green");
    const IncrementCountActionName =
      "TimeWarp.Architecture.Features.Counters.CounterState+IncrementCounterActionSet+Action, Web.Spa";
    timeWarpState.DispatchRequest(IncrementCountActionName, { amount: 7 }).then();
  },
};
