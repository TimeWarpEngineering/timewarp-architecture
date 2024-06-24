
  import { DotNetReference } from './DotNetReference';
  import { ReduxDevTools } from "./ReduxDevTools";

  export declare class TimeWarpState {
    jsonRequestHandler: DotNetReference;
    reduxDevTools: ReduxDevTools;

    /**
     * Dispatches a JSON request to the .NET backend.
     * @param {string} requestTypeFullName - The full name of the request type.
     * @param {any} request - The request payload.
     */
    DispatchRequest(requestTypeFullName: string, request: unknown): Promise<void>;
  }

  export declare const timeWarpState: TimeWarpState;

