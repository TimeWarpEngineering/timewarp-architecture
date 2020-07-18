import { TimeWarpBlazor } from "./TimeWarpBlazor";

declare global {
  declare var BlazorState: BlazorState;

  interface BlazorState {
    DispatchRequest(requestTypeFullName: string, request: any): Promise<void>
  }

  interface Window {
    _TimeWarpBlazor_: TimeWarpBlazor | undefined
    BlazorState: BlazorState
  }
}

export { };