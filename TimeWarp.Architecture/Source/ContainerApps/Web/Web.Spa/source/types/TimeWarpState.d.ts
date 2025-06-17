declare module "/_content/TimeWarp.State/js/TimeWarpState.*" {
  import { DotNetReference } from "./DotNetReference.js";
  import { ReduxDevTools } from "/_content/TimeWarp.State/js/ReduxDevTools.*";

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
}
