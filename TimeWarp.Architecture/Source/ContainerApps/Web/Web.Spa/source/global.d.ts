declare module "/_content/TimeWarp.State/js/*" {
  export interface LogStyles {
    info: string;
    success: string;
    warning: string;
    error: string;
    function: string;
  }

  export const logStyles: LogStyles;
  export type LogLevel = keyof LogStyles;

  export enum LogAction {
    Begin = 0,
    End = 1
  }

  export const log: (tag: string, message: string, level?: LogLevel, action?: LogAction) => void;
}

