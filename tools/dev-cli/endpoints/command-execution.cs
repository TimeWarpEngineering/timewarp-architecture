#region Purpose
// Shared result handling for dev-cli dotnet/shell commands
#endregion
#region Design
// Passthrough streams child stdout/stderr live (default). Capture hides output unless
// --quiet is set; failures still dump Combined.
#endregion

namespace DevCli.Commands;

internal static class CommandExecution
{
  internal static bool ReportPassthrough(ITerminal terminal, ExecutionResult result, string failureMessage)
  {
    if (!result.IsSuccess)
    {
      terminal.WriteErrorLine(failureMessage.Red());
      Environment.ExitCode = result.ExitCode == 0 ? 1 : result.ExitCode;
      return false;
    }

    return true;
  }

  internal static bool ReportCapture(ITerminal terminal, CommandOutput result, string failureMessage)
  {
    if (!result.Success)
    {
      terminal.WriteErrorLine(result.Combined);
      terminal.WriteErrorLine(failureMessage.Red());
      Environment.ExitCode = 1;
      return false;
    }

    return true;
  }
}