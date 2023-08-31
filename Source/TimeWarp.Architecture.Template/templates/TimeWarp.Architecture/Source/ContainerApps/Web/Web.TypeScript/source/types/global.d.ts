import { BlazorDualMode } from "./BlazorDualMode";

interface BlazorMethodReference {
  invokeMethodAsync: (methodName: string, ...args: any[]) => Promise<void>;
}

interface DisposeHandler {
  dispose: () => void;
}

declare global {
  let BlazorState: BlazorState;
  function boot(): Promise<void>;

  interface BlazorState {
    DispatchRequest(requestTypeFullName: string, request: any): Promise<void>;
  }

  interface Blazor {
    start(): Promise<void>;
  }

  interface Window {
    BlazorDualMode: BlazorDualMode | undefined;
    BlazorState: BlazorState;
    Blazor: Blazor;
    Spa: typeof import("../Spa").Spa;
    NotifyLossOfInterest: (elementId: string, blazorMethodReference: BlazorMethodReference) => DisposeHandler;  // Added line
  }
}

export {};
