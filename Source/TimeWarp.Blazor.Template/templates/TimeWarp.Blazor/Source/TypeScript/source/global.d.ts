import { TimeWarpBlazor } from "./TimeWarpBlazor";

declare global {
  var _TimeWarpBlazor_: TimeWarpBlazor;
  var BlazorState: BlazorState;

  interface BlazorState {
    DispatchRequest(requestTypeFullName: string, request: any): Promise<void>
  }

  interface Window {
    _TimeWarpBlazor_: TimeWarpBlazor
  }
}
