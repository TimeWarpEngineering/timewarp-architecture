import { CompositionRoot } from "./CompositionRoot.js";

declare global {
  declare let BlazorState: BlazorState;
  declare function boot(): Promise<void>;

  interface BlazorState {
    DispatchRequest(requestTypeFullName: string, request: any): Promise<void>;
  }

  interface Blazor {
    start(): Promise<void>;
  }

  interface Window {
    CompositionRoot: CompositionRoot | undefined;
    BlazorState: BlazorState;
    Blazor: Blazor;
  }
}

export {};
