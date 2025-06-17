declare module "/_content/TimeWarp.State/js/Logger.*" {
  export declare interface LogStyles {
    info: string;
    success: string;
    warning: string;
    error: string;
    function: string;
  }

  export declare const logStyles: LogStyles;
  export declare type LogLevel = keyof LogStyles;

  export declare enum LogAction {
    Begin = 0,
    End = 1
  }

  export declare const log: (tag: string, message: string, level?: LogLevel, action?: LogAction) => void;

}
