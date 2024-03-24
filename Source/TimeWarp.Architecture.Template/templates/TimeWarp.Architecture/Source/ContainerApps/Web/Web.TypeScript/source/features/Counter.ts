// Counter.ts
import { blazorState } from "/_content/Blazor-State/js/BlazorState.js";
import { Spa } from "../Spa.js";

export class Counter extends Spa {
  public static DispatchIncrementCountAction = () => {
    console.log("%cdispatchIncrementCountAction", "color: green");
    const IncrementCountActionName =
      "TimeWarp.Architecture.Features.Counters.CounterState+IncrementCounter+Action, Web.Spa";
    blazorState.DispatchRequest(IncrementCountActionName, { amount: 7 }).then();
  };
}
