import { BlazorState } from "./BlazorState.js";
type traceType = (action: unknown) => string;
export declare class ReduxDevTools {
  IsEnabled: boolean;
  DevTools: unknown;
  Extension: unknown;
  Config: {
    name: string;
    trace: boolean | traceType;
    features: {
      pause: boolean;
      lock: boolean;
      persist: boolean;
      export: boolean;
      import: boolean;
      jump: boolean;
      skip: boolean;
      reorder: boolean;
      dispatch: boolean;
      test: boolean;
    };
  };
  BlazorState: BlazorState;
  StackTrace: string | undefined;
  constructor(reduxDevToolsOptions: unknown);
  Init(): void;
  GetExtension(): unknown;
  GetDevTools(): unknown;
  MapRequestType(message: unknown): unknown;
  MessageHandler: (message: unknown) => void;
  ReduxDevToolsDispatch(action: unknown, state: unknown, stackTrace: unknown): unknown;
  GetStackTraceForAction(action: unknown): string;
}
export {};
