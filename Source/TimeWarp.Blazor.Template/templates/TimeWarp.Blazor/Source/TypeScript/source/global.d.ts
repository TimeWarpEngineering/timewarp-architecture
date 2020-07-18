import { TimeWarpBlazor } from "./TimeWarpBlazor";

declare global {
  declare var BlazorState: BlazorState;
  declare function boot(): Promise<void>;

  interface BlazorState {
    DispatchRequest(requestTypeFullName: string, request: any): Promise<void>
  }

  interface Blazor {
    start(): Promise<void>
  }

  interface Window {
    _TimeWarpBlazor_: TimeWarpBlazor | undefined
    BlazorState: BlazorState
    Blazor: Blazor
  }
}

export { };