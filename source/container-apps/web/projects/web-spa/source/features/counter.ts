// #region Purpose
// Counter.ts — demonstrates dispatching a TimeWarp.State action from JavaScript.
// #endregion
//
// #region Design
// Exposed as window.Spa.Counter; the CounterPage.razor button's JavaScript onclick calls it
// directly (timewarp-state test-app shape) — no C# handler round-trips through IJSRuntime (TWA0026).
// Plain object (not a class) so the window.Spa namespace stays traversable by Blazor's
// string-identifier interop for any future caller.
// IncrementCountActionName is the alias Program.AllowJavaScriptDispatch allows
// (CounterState.IncrementCounterActionSet.JavaScriptAlias). JavaScript dispatch is opt-in since
// TimeWarp.State 12.0.0-beta.8; the alias keeps the CLR type name out of this file.
// counter-js-dispatch tests read this file and dispatch what it sends.
// #endregion
import { timeWarpState } from "/_content/TimeWarp.State/js/timewarp-state.js";

export const Counter = {
  DispatchIncrementCountAction: () => {
    console.log("%cdispatchIncrementCountAction", "color: green");
    const IncrementCountActionName = "Counter.Increment";
    timeWarpState.DispatchRequest(IncrementCountActionName, { amount: 7 }).then();
  },
};
