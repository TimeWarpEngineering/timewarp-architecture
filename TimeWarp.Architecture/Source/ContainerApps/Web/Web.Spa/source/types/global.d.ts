interface BlazorMethodReference {
  invokeMethodAsync: (methodName: string, ...args: unknown[]) => Promise<void>;
}

interface DisposeHandler {
  dispose: () => void;
}

declare global {
  let TimeWarpState: TimeWarpState;
  function boot(): Promise<void>;

  interface TimeWarpState {
    DispatchRequest(requestTypeFullName: string, request: unknown): Promise<void>;
  }

  interface Blazor {
    start(): Promise<void>;
  }

  interface Window {
    TimeWarpState: TimeWarpState;
    Blazor: Blazor;
    Spa: typeof import("../Spa").Spa;
  }
}

export {};
