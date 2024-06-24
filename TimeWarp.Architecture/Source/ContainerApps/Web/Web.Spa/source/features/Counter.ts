// Counter.ts
import { timeWarpState } from "/_content/TimeWarp.State/js/TimeWarpState.js";
import { Spa } from "../Spa.js";

export class Counter extends Spa {
  public static DispatchIncrementCountAction = () => {
    console.log("%cdispatchIncrementCountAction", "color: green");
    const IncrementCountActionName =
      "TimeWarp.Architecture.Features.Counters.CounterState+IncrementCounter+Action, Web.Spa";
    timeWarpState.DispatchRequest(IncrementCountActionName, { amount: 7 }).then();
  };
}
