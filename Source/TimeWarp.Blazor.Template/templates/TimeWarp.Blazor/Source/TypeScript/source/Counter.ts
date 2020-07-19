export class Counter {
  private BlazorState: BlazorState;

  constructor(aBlazorState: BlazorState) {
    this.BlazorState = aBlazorState;
  }

  DispatchIncrementCountAction() {
    console.log("%cdispatchIncrementCountAction", "color: green");

    const IncrementCountActionName =
      "TimeWarp.Blazor.Features.Counters.CounterState+IncrementCounterAction, TimeWarp.Blazor.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    this.BlazorState.DispatchRequest(IncrementCountActionName, { amount: 7 });
  }
}
