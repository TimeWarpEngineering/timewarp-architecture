#region Purpose
// AppHost appsettings.json parameter names and DNS-1123 label check for generated apps.
#endregion
#region Design
// Task 288: the appNameKebab derived symbol (template.json) rewrites the AppHost's committed
// k8s-namespace / helm-release-name / registry-repository to the app's DNS-1123 kebab name. The
// template engine cannot fail generation on an invalid label, so the assert that uses this check is
// the loud failure.
#endregion

namespace DevCli.Services;

/// <summary>
/// Constants and label validation for the generated AppHost deploy parameters.
/// </summary>
internal static partial class TemplateSmokeAppHostParameters
{
  /// <summary>AppHost appsettings.json path, relative to a generated app root.</summary>
  public const string AppHostAppSettingsRelativePath = "source/container-apps/aspire/projects/aspire-app-host/appsettings.json";

  /// <summary>The committed AppHost Parameters the appNameKebab template symbol sets to the app's kebab name (task 288).</summary>
  public static readonly string[] AppNameKebabParameters = ["k8s-namespace", "helm-release-name", "registry-repository"];

  [GeneratedRegex("^[a-z0-9]([-a-z0-9]{0,61}[a-z0-9])?$")]
  private static partial Regex DnsLabel();

  /// <summary>True when <paramref name="value"/> is a Kubernetes DNS-1123 label (≤ 63 chars, [a-z0-9-], alphanumeric ends).</summary>
  public static bool IsDnsLabel(string value) => DnsLabel().IsMatch(value);
}
