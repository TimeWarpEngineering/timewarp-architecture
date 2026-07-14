#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru
#:property NoWarn=CA2007

using TimeWarp.Amuru;

// Amuru 1.0.0 removed the Git helper class; ask git directly for the root.
CommandOutput rootOutput = await Shell.Builder("git")
  .WithArguments("rev-parse", "--show-toplevel")
  .WithNoValidation()
  .CaptureAsync();

string root = rootOutput.Stdout.Trim();
if (!rootOutput.Success || root.Length == 0)
{
  return 0;
}

await Shell.Builder("ganda")
  .WithArguments("memsearch", "index-repo", "--background")
  .WithWorkingDirectory(root)
  .WithNoValidation()
  .RunAsync();
return 0;
