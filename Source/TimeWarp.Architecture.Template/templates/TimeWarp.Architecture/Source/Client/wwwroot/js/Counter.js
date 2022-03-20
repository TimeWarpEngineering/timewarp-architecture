export class Counter {
    constructor(aBlazorState) {
        this.BlazorState = aBlazorState;
    }
    DispatchIncrementCountAction() {
        console.log("%cdispatchIncrementCountAction", "color: green");
        const IncrementCountActionName = "TimeWarp.Architecture.Features.Counters.CounterState+IncrementCounterAction, TimeWarp.Architecture.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        this.BlazorState.DispatchRequest(IncrementCountActionName, { amount: 7 });
    }
}
