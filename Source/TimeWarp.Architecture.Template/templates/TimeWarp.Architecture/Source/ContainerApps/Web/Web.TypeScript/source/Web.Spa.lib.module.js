import { blazorState } from "/_content/Blazor-State/js/BlazorState.js";

const dispatchIncrementCountAction = () => {
  console.log("%cdispatchIncrementCountAction", "color: green");
  const IncrementCountActionName =
    "TimeWarp.Architecture.Features.Counters.Spa.CounterState+IncrementCounterAction, Web.Spa, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
  blazorState.DispatchRequest(IncrementCountActionName, { amount: 7 });
};

export function beforeStart(options, extensions) {
  console.log("****beforeStart Web.Spa ****");

  window["DispatchIncrementCountAction"] = dispatchIncrementCountAction;
}

export function afterStarted(blazor) {
  console.log("****afterStarted  Web.Spa ****");
}
