#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru
#:package TimeWarp.Amuru.Tools
#:property NoWarn=CA2007

using TimeWarp.Amuru;

string? root = Git.FindRoot();
if (root is not null)
{
  await Shell.Builder("ganda")
    .WithArguments("memsearch", "index-repo", "--background")
    .WithWorkingDirectory(root)
    .WithNoValidation()
    .RunAsync();
}
