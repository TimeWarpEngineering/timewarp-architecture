#region Purpose
// Gates the stale migration script handling and the preflight scopes.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class StaleScript_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StaleScript_Given_>();

  // EF's idempotent script output for two migrations (Npgsql provider).
  private const string Script = """
    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
        "MigrationId" character varying(150) NOT NULL,
        "ProductVersion" character varying(32) NOT NULL,
        CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
    );

    START TRANSACTION;
    DO $EF$
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804154245_InitialPostgresModel') THEN
        CREATE TABLE "Todo" ("Id" uuid NOT NULL);
        END IF;
    END $EF$;

    DO $EF$
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804154245_InitialPostgresModel') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260804154245_InitialPostgresModel', '11.0.0');
        END IF;
    END $EF$;

    DO $EF$
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923180147_AddCredentialAccountHint') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260923180147_AddCredentialAccountHint', '11.0.0');
        END IF;
    END $EF$;
    COMMIT;
    """;

  private static readonly string[] Folder =
  [
    "20260804154245_InitialPostgresModel.cs",
    "20260804154245_InitialPostgresModel.Designer.cs",
    "20260923180147_AddCredentialAccountHint.cs",
    "20260923180147_AddCredentialAccountHint.Designer.cs",
    "20261010075816_AddFeedbackAttachments.cs",
    "20261010075816_AddFeedbackAttachments.Designer.cs",
    "PostgresDbContextModelSnapshot.cs",
  ];

  public static Task ScriptIds_Should_ComeFromTheHistoryInserts()
  {
    DeployStaleScript.ParseScriptMigrationIds(Script)
      .ShouldBe(["20260804154245_InitialPostgresModel", "20260923180147_AddCredentialAccountHint"]);
    DeployStaleScript.ParseScriptMigrationIds("").ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task SourceIds_Should_SkipDesignerFilesAndTheSnapshot()
  {
    DeployStaleScript.ParseSourceMigrationIds(Folder).ShouldBe(
    [
      "20260804154245_InitialPostgresModel",
      "20260923180147_AddCredentialAccountHint",
      "20261010075816_AddFeedbackAttachments",
    ]);
    return Task.CompletedTask;
  }

  public static Task Stale_Should_ListTheSourceIdsTheScriptLacks()
  {
    string[] missing = DeployStaleScript.MigrationsMissingFromScript(
      DeployStaleScript.ParseSourceMigrationIds(Folder), DeployStaleScript.ParseScriptMigrationIds(Script));
    missing.ShouldBe(["20261010075816_AddFeedbackAttachments"]);
    string warning = DeployStaleScript.StaleScriptWarning(AspireDeploy.Kubernetes, missing);
    warning.ShouldStartWith("⚠ Published script is stale: source has 1 migration(s) the script does not");
    warning.ShouldContain("(20261010075816_AddFeedbackAttachments)");
    warning.ShouldContain("Run `dev publish kubernetes` first");
    warning.ShouldContain("then redeploy so the app and schema match.");
    return Task.CompletedTask;
  }

  public static Task Current_Should_HaveNothingMissing()
  {
    DeployStaleScript.MigrationsMissingFromScript(DeployStaleScript.ParseSourceMigrationIds(Folder[..4]), DeployStaleScript.ParseScriptMigrationIds(Script))
      .ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Yes_Should_RefuseAStaleScriptUnlessAllowStale()
  {
    DeployStaleScript.RefuseStaleUnattended(stale: true, yes: true, allowStale: false).ShouldBeTrue();
    DeployStaleScript.RefuseStaleUnattended(stale: true, yes: true, allowStale: true).ShouldBeFalse();
    DeployStaleScript.RefuseStaleUnattended(stale: true, yes: false, allowStale: false).ShouldBeFalse();
    DeployStaleScript.RefuseStaleUnattended(stale: false, yes: true, allowStale: false).ShouldBeFalse();
    string refusal = DeployStaleScript.StaleUnattendedRefusal(AspireDeploy.Compose);
    refusal.ShouldContain("dev publish compose");
    refusal.ShouldContain("--allow-stale");
    return Task.CompletedTask;
  }

  public static Task Report_Should_SayRelativeToThePublishedScript()
  {
    DeployStaleScript.QualifyRelativeToPublishedScript(["Up to date: no pending migrations (2 applied, latest b)"])
      .ShouldBe(["Up to date relative to the published script: no pending migrations (2 applied, latest b)"]);
    DeployStaleScript.QualifyRelativeToPublishedScript(["Applied 1 migration(s):", "  b", "Already applied: 1 · Database now at b"])
      .ShouldBe(["Applied 1 migration(s) relative to the published script:", "  b", "Already applied: 1 · Database now at b"]);
    return Task.CompletedTask;
  }

  public static Task SourceFolders_Should_ComeFromTheAppHostsSiblingApps()
  {
    string root = Path.Combine(Path.GetTempPath(), "stale-script-" + Guid.NewGuid().ToString("N"));
    try
    {
      string appHost = Path.Combine(root, "source", "container-apps", "aspire", "projects", "aspire-app-host", "aspire-app-host.csproj");
      Directory.CreateDirectory(Path.GetDirectoryName(appHost)!);
      File.WriteAllText(appHost, "");
      Directory.CreateDirectory(Path.Combine(root, "source", "container-apps", "web", "platform"));

      // No postgres flag: no migrations folder, so the check is skipped.
      DeployStaleScript.FindSourceMigrationFolders(appHost, root).ShouldBeEmpty();

      string migrations = Path.Combine(root, "source", "container-apps", "web", DeployStaleScript.SourceMigrationsSubpath);
      Directory.CreateDirectory(migrations);
      DeployStaleScript.FindSourceMigrationFolders(appHost, root).ShouldBe([migrations]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}
