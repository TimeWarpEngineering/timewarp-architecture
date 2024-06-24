interface BlazorMethodReference {
  invokeMethodAsync: (methodName: string, ...args: unknown[]) => Promise<void>;
}

interface DisposeHandler {
  dispose: () => void;
}

declare global {
  function boot(): Promise<void>;

  interface Blazor {
    start(): Promise<void>;
  }

  interface Window {
    TimeWarpState: typeof import("/_content/TimeWarp.State/js/TimeWarpState.js").TimeWarpState;
    Blazor: Blazor;
    Spa: typeof import("../Spa").Spa;
  }
}

export {};
