declare module "/_content/TimeWarp.State/js/timewarp-state.*" {
  import { DotNetReference } from "./dot-net-reference.js";
  import { ReduxDevTools } from "/_content/TimeWarp.State/js/redux-dev-tools.*";

  export declare class TimeWarpState {
    jsonRequestHandler: DotNetReference;
    reduxDevTools: ReduxDevTools;

    /**
     * Dispatches a JSON request to the .NET backend.
     * @param {string} requestTypeFullName - An allowed action's alias, full name, or assembly-qualified name (AddJavaScriptDispatch).
     * @param {any} request - The request payload.
     */
    DispatchRequest(requestTypeFullName: string, request: unknown): Promise<void>;
  }

  export declare const timeWarpState: TimeWarpState;
}
