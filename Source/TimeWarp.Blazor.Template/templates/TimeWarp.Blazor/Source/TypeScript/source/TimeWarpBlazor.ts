export class TimeWarpBlazor {

  HelloWorld() {
    console.log("Hello World");
  }

  DispatchIncrementCountAction(){
    console.log("%cdispatchIncrementCountAction", "color: green");

    const IncrementCountActionName =
      "TimeWarp.Blazor.Features.Counters.CounterState+IncrementCounterAction, TimeWarp.Blazor.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    BlazorState.DispatchRequest(IncrementCountActionName, { amount: 7 });
  }; 
}