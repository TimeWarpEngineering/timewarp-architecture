#region Purpose
// Operator text for `dev deploy` / `dev deprovision`: preflight report, plan, confirmation lines
// and the manual cleanup printed after a failed `aspire destroy`.
#endregion

#region Design
// Text only. The manual removal for a target is what the operator types when `aspire destroy` has no record:
// for aca it is `az group delete` (az asks for confirmation) plus purging the soft-deleted Key Vault: a
// deleted vault keeps its name reserved, and the Bicep derives the name from the resource group, so a
// redeploy into a same-named group fails until it is purged.
//
// `dev deprovision` prints the manual removal on failure (BuildManualCleanupLines) and exits with Aspire's exit code.
// The dev CLI never inspects Aspire's own deployment state and never runs the manual removal itself.
//
// Forwarded parameters are printed in the plan, so a secret never appears in BuildDeployPlanLines: a secret name on the
// parameter list is refused (AspireDeploy.RejectSecretsOnParameters).
#endregion

namespace DevCli.Services;

/// <summary>Operator text for <c>dev deploy</c> / <c>dev deprovision</c>: preflight report, plan, confirmation lines and manual cleanup.</summary>
internal static class AspireDeployText
{
  /// <summary>One report for every preflight problem found, so the operator fixes them in one pass.</summary>
  internal static string[] BuildPreflightReport(string verb, IReadOnlyList<string> problems) =>
  [
    $"{verb} preflight failed ({problems.Count} problem{(problems.Count == 1 ? "" : "s")}); nothing was run:",
    .. problems.Select(problem => $"- {problem}"),
  ];

  /// <summary>What `dev deploy` is about to do, printed before the confirmation.</summary>
  internal static string[] BuildDeployPlanLines(string appHostProject, DeployTarget target, string preflightDetail, IReadOnlyList<ResolvedDeployParameter> parameters) =>
  [
    $"dev deploy → aspire deploy ({target.Name})",
    $"  AppHost:     {appHostProject}",
    $"  Environment: {AspireDeploy.DeployEnvironment}",
    $"  Target:      Publish:Target={target.Name}",
    .. AspireDeployParameters.BuildParameterLines(parameters),
    $"  {preflightDetail}",
  ];

  /// <summary>
  /// Same plan as the parameter overload. Secret values are not printed.
  /// A secret name on <paramref name="parameters"/> is refused.
  /// </summary>
  internal static string[] BuildDeployPlanLines(
    string appHostProject,
    DeployTarget target,
    string preflightDetail,
    IReadOnlyList<ResolvedDeployParameter> parameters,
    IReadOnlyList<ResolvedDeployParameter> secrets)
  {
    AspireDeploy.RejectSecretsOnParameters(parameters, secrets);
    return BuildDeployPlanLines(appHostProject, target, preflightDetail, parameters);
  }

  internal const string DeployConfirmationRefusal =
    "Not deploying: no confirmation. Re-run with --yes to deploy non-interactively, or from a terminal to answer the prompt.";

  /// <summary>Printed when the operator answers anything but yes at the deploy prompt.</summary>
  internal const string DeployDeclined = "Not deploying: declined at the prompt. Nothing was run.";

  internal const string DestroyConfirmationRefusal =
    "Not destroying: no confirmation. Re-run with --yes to destroy non-interactively, or from a terminal to answer Aspire's prompt.";

  /// <summary>Printed after a failed <c>aspire destroy</c>: how to remove the deployment by hand.</summary>
  internal static string[] BuildManualCleanupLines(DeployTarget target, string containerRuntime) =>
  [
    "`aspire destroy` only knows deployments recorded on the machine (and checkout) that ran `aspire deploy`. If the stack is still running, remove it by hand — this deletes its data:",
    .. ManualRemovalLines(target, containerRuntime),
  ];

  private static string[] ManualRemovalLines(DeployTarget target, string containerRuntime) =>
    target == AspireDeploy.ContainerApps
      ?
      [
        $"  az group list --query \"[].name\" --output tsv                # find the resource group ({AspireDeployAzure.AzureResourceGroupVariable}, or the name aspire deploy asked for)",
        "  az group delete --name <resource-group>                      # az asks for confirmation; deletes every resource in the group",
        .. KeyVaultPurgeLines,
      ]
      : target == AspireDeploy.ContainerAppsNeon
      ?
      [
        $"  az group list --query \"[].name\" --output tsv                # find the resource group ({AspireDeployAzure.AzureResourceGroupVariable}, or the name aspire deploy asked for)",
        "  az group delete --name <resource-group>                      # az asks for confirmation; deletes every resource in the group",
        "  The Neon project is not part of the resource group and is not deleted.",
      ]
      : target == AspireDeploy.Kubernetes
      ?
      [
        "  helm list --all-namespaces                                  # find the release and namespace",
        $"  helm uninstall <release> --namespace <namespace>            # AppHost parameters {AspireDeployParameters.HelmReleaseNameParameter} and {AspireDeployParameters.KubernetesNamespaceParameter}",
        $"  kubectl delete pvc {AspireDeploy.PostgresClaimName} --namespace <namespace>",
      ]
      :
      [
        $"  {containerRuntime} compose ls                                     # find the project",
        $"  {containerRuntime} compose --project-name <project> down --volumes",
      ];

  // A deleted Key Vault is soft-deleted and keeps its name; the Bicep names it from the resource group.
  private static readonly string[] KeyVaultPurgeLines =
  [
    "Then purge the soft-deleted Key Vault, or a redeploy into a same-named resource group fails on the reserved vault name:",
    $"  az keyvault list-deleted --query \"[?properties.tags.\\\"aspire-resource-name\\\"=='{AspireDeploy.PostgresKeyVaultTag}'].name\" --output tsv",
    "  az keyvault purge --name <vault>",
  ];

  /// <summary>Kubernetes: the claim can outlive `helm uninstall`; aca: the Key Vault outlives the group. Tell the operator how to check.</summary>
  internal static string[] BuildPostDestroyLines(DeployTarget target) =>
    target == AspireDeploy.ContainerApps
      ? KeyVaultPurgeLines
      : target == AspireDeploy.ContainerAppsNeon
      ?
      [
        "The Azure resource group is gone. The Neon project is not deleted; suspend or delete it in the Neon console if you are done with it.",
      ]
      : target == AspireDeploy.Kubernetes
      ?
      [
        "Check that the postgres claim is gone: kubectl get pvc --namespace <namespace>",
        $"If {AspireDeploy.PostgresClaimName} remains, delete it: kubectl delete pvc {AspireDeploy.PostgresClaimName} --namespace <namespace>",
      ]
      : [];
}
