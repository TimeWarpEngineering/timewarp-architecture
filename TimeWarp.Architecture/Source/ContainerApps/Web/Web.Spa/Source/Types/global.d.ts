import { Spa } from "../Spa.js";
import type { TimeWarpState } from "/_content/TimeWarp.State/js/TimeWarpState.js";
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
    TimeWarpState: TimeWarpState;
    Blazor: Blazor;
    Spa: Spa;
  }
}

export {};
