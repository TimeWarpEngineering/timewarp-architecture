#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru
#:property NoWarn=CA2007

using TimeWarp.Amuru;

string? root = Git.FindRoot();
if (root is null)
{
  return 0;
}

await Shell.Builder("ganda")
  .WithArguments("memsearch", "index-repo", "--background")
  .WithWorkingDirectory(root)
  .WithNoValidation()
  .RunAsync();
return 0;