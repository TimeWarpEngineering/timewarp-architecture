#region Purpose
// Pins dev lint-skills to the vally version file and to the CI skill-lint job.
#endregion

namespace SkillSpecLint_;

public class PinnedVersion_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PinnedVersion_Given_>();

  public static Task VersionFile_Should_BeASemver()
  {
    string? version = SkillSpecLint.ReadPinnedVersion(SkillSpecLintRepo.Root());
    version.ShouldNotBeNull();
    SkillSpecLint.ParseVallyVersion(version).ShouldBe(version);
    return Task.CompletedTask;
  }

  public static Task MissingFile_Should_ReturnNull()
  {
    string root = Directory.CreateTempSubdirectory("skill-spec-lint-").FullName;
    try
    {
      SkillSpecLint.ReadPinnedVersion(root).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task CommentInPinFile_Should_ReturnNull()
  {
    string root = Directory.CreateTempSubdirectory("skill-spec-lint-").FullName;
    try
    {
      string path = Path.Combine(root, SkillSpecLint.VersionFileRelativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "0.19.0\n# latest\n");
      SkillSpecLint.ReadPinnedVersion(root).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}

public class ParseVallyVersion_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ParseVallyVersion_Given_>();

  public static Task VersionLine_Should_Parse()
  {
    SkillSpecLint.ParseVallyVersion("0.19.0").ShouldBe("0.19.0");
    SkillSpecLint.ParseVallyVersion("0.19.0\n").ShouldBe("0.19.0");
    SkillSpecLint.ParseVallyVersion("vally 0.6.0").ShouldBe("0.6.0");
    SkillSpecLint.ParseVallyVersion("v1.2.3-preview.1").ShouldBe("1.2.3-preview.1");
    SkillSpecLint.ParseVallyVersion("not a version").ShouldBeNull();
    SkillSpecLint.ParseVallyVersion(null).ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class Arguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Arguments_Given_>();

  public static Task SkillLint_Should_MatchCi()
  {
    SkillSpecLint.SkillLintArguments.ShouldBe(["lint", "skills"]);
    return Task.CompletedTask;
  }

  public static Task EvalLint_Should_PassTheSpecPath()
  {
    SkillSpecLint.EvalLintArguments("skills/tw-web-api-contracts/evals/eval.yaml")
      .ShouldBe(["lint", "--eval-spec", "skills/tw-web-api-contracts/evals/eval.yaml"]);
    return Task.CompletedTask;
  }

  public static Task Repo_Should_FindTheContractsEvalSpec()
  {
    SkillSpecLint.EvalSpecRelativePaths(SkillSpecLintRepo.Root())
      .ShouldBe(["skills/tw-web-api-contracts/evals/eval.yaml"]);
    return Task.CompletedTask;
  }

  public static Task NestedEvalYaml_Should_BeIgnored()
  {
    string root = Directory.CreateTempSubdirectory("skill-spec-lint-").FullName;
    try
    {
      string spec = Path.Combine(root, "skills", "demo", "evals", "eval.yaml");
      Directory.CreateDirectory(Path.GetDirectoryName(spec)!);
      File.WriteAllText(spec, "name: demo\n");
      string nested = Path.Combine(root, "skills", "demo", "references", "evals", "eval.yaml");
      Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
      File.WriteAllText(nested, "name: nested\n");
      SkillSpecLint.EvalSpecRelativePaths(root).ShouldBe(["skills/demo/evals/eval.yaml"]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}

public class MissingVally_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<MissingVally_Given_>();

  public static Task Message_Should_SayHowToInstallThePin()
  {
    string message = SkillSpecLint.MissingExecutableMessage("0.19.0");
    message.ShouldContain("not on PATH");
    message.ShouldContain("Node.js");
    message.ShouldContain("https://nodejs.org/");
    message.ShouldContain(SkillSpecLint.InstallCommand("0.19.0"));
    message.ShouldContain("npm install -g @microsoft/vally-cli@0.19.0");
    return Task.CompletedTask;
  }

  public static Task DifferentVersion_Should_NameBothAndTheInstall()
  {
    string message = SkillSpecLint.VersionMismatchMessage("0.6.0", "0.19.0");
    message.ShouldContain("0.6.0");
    message.ShouldContain("@microsoft/vally-cli@0.19.0");
    message.ShouldContain(SkillSpecLint.VersionFileRelativePath);
    return Task.CompletedTask;
  }

  public static Task UnreadableVersion_Should_StillGiveTheInstall()
  {
    string message = SkillSpecLint.VersionMismatchMessage(null, "0.19.0");
    message.ShouldContain("did not report a version");
    message.ShouldContain("npm install -g @microsoft/vally-cli@0.19.0");
    return Task.CompletedTask;
  }

  public static Task EmptyPath_Should_NotFindVally()
  {
    string root = Directory.CreateTempSubdirectory("skill-spec-lint-").FullName;
    try
    {
      string bin = Path.Combine(root, "bin");
      Directory.CreateDirectory(bin);
      string vally = Path.Combine(bin, "vally");
      File.WriteAllText(vally, "#!/bin/sh\n");
      SkillSpecLint.IsOnPath("vally", "").ShouldBeFalse();
      SkillSpecLint.IsOnPath("vally", bin).ShouldBeTrue();
      SkillSpecLint.IsOnPath("vally", Path.Combine(root, "missing")).ShouldBeFalse();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}

public class Workflow_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Workflow_Given_>();

  public static Task RepoWorkflow_Should_CallLintSkillsAtThePin()
  {
    string workflow = File.ReadAllText(Path.Combine(SkillSpecLintRepo.Root(), ".github/workflows/workflow.yml"));
    SkillSpecLint.WorkflowDriftError(workflow).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task DirectVallyLint_Should_Fail()
  {
    string workflow = AgreeingWorkflow() + "\n        run: vally lint skills\n";
    string? error = SkillSpecLint.WorkflowDriftError(workflow);
    error.ShouldNotBeNull();
    error.ShouldContain("dev lint-skills");
    return Task.CompletedTask;
  }

  public static Task CommentedDirectLint_Should_BeIgnored()
  {
    string workflow = AgreeingWorkflow() + "\n        # run: vally lint skills\n";
    SkillSpecLint.WorkflowDriftError(workflow).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task UnpinnedInstall_Should_Fail()
  {
    string workflow = AgreeingWorkflow().Replace(
      "@microsoft/vally-cli@$version",
      "@microsoft/vally-cli",
      StringComparison.Ordinal);
    string? error = SkillSpecLint.WorkflowDriftError(workflow);
    error.ShouldNotBeNull();
    error.ShouldContain(SkillSpecLint.VersionFileRelativePath);
    return Task.CompletedTask;
  }

  public static Task LiteralVersionInstall_Should_Fail()
  {
    string workflow = AgreeingWorkflow().Replace(
      "@microsoft/vally-cli@$version",
      "@microsoft/vally-cli@0.19.0",
      StringComparison.Ordinal);
    SkillSpecLint.WorkflowDriftError(workflow).ShouldNotBeNull();
    return Task.CompletedTask;
  }

  public static Task MissingDevCliPathFilter_Should_Fail()
  {
    string workflow = AgreeingWorkflow().Replace("^tools/dev-cli/", "^tools/other/", StringComparison.Ordinal);
    string? error = SkillSpecLint.WorkflowDriftError(workflow);
    error.ShouldNotBeNull();
    error.ShouldContain("tools/dev-cli/");
    return Task.CompletedTask;
  }

  private static string AgreeingWorkflow() =>
    """
    version=$(tr -d '[:space:]' < tools/dev-cli/vally-cli.version)
    npm install -g "@microsoft/vally-cli@$version"
    ^tools/dev-cli/
    dotnet run tools/dev-cli/dev.cs -- lint-skills
    """;

}

file static class SkillSpecLintRepo
{
  internal static string Root()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, SkillSpecLint.VersionFileRelativePath)))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }
}
