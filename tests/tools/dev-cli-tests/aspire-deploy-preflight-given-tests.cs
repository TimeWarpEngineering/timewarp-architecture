#region Purpose
// Gates the Helm / kubectl preflight refusals of `dev deploy` without deploying.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public class Preflight_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Preflight_Given_>();

  public static Task Helm42OrLater_Should_Pass()
  {
    AspireDeployCluster.ValidateHelm(true, "v4.2.0+g1a2b3c4\n").ShouldBeNull();
    AspireDeployCluster.ValidateHelm(true, "v4.3.0+gbec5b06").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task OlderHelm_Should_BeRefusedWithTheInstalledVersion()
  {
    string? error = AspireDeployCluster.ValidateHelm(true, "v3.16.2+g13654a5");
    error.ShouldNotBeNull();
    error.ShouldContain("4.2");
    error.ShouldContain("installed: 3.16.2");
    return Task.CompletedTask;
  }

  public static Task MissingOrGarbledHelm_Should_BeRefused()
  {
    AspireDeployCluster.ValidateHelm(false, "").ShouldNotBeNull().ShouldContain("not found on PATH");
    AspireDeployCluster.ValidateHelm(true, "helm: unknown").ShouldNotBeNull().ShouldContain("Could not determine");
    return Task.CompletedTask;
  }

  public static Task KubectlContext_Should_BeReturnedTrimmed()
  {
    AspireDeployCluster.ParseKubectlContext(true, "kind-simple\n").ShouldBe("kind-simple");
    return Task.CompletedTask;
  }

  public static Task NoKubectlContext_Should_BeRefused()
  {
    AspireDeployCluster.ParseKubectlContext(false, "").ShouldBeNull();
    AspireDeployCluster.ParseKubectlContext(true, "   ").ShouldBeNull();
    AspireDeployCluster.NoKubectlContextMessage.ShouldContain("kubectl config use-context");
    return Task.CompletedTask;
  }

  public static Task AzureAccount_Should_ParseIdAndName()
  {
    AzureAccount account = AspireDeployAzure.ParseAzureAccount(true, "00000000-1111-2222-3333-444444444444\nContoso Dev\n").ShouldNotBeNull();
    account.Id.ShouldBe("00000000-1111-2222-3333-444444444444");
    account.Name.ShouldBe("Contoso Dev");
    return Task.CompletedTask;
  }

  public static Task NoAzureLogin_Should_BeRefused()
  {
    AspireDeployAzure.ParseAzureAccount(false, "").ShouldBeNull();
    AspireDeployAzure.ParseAzureAccount(true, "Please run 'az login' to setup account.").ShouldBeNull();
    AspireDeployAzure.NoAzureLoginMessage.ShouldContain("az login");
    return Task.CompletedTask;
  }

  private static readonly AzureAccount CliAccount = new("00000000-1111-2222-3333-444444444444", "Contoso Dev");

  public static Task NoConfiguredSubscription_Should_FallBackToTheAzCliAndPassItToAspire()
  {
    AzureSubscriptionChoice choice = AspireDeployAzure.ChooseAzureSubscription(null, " ", CliAccount);
    choice.SubscriptionId.ShouldBe(CliAccount.Id);
    choice.PassToAspire.ShouldBeTrue();
    choice.Detail.ShouldContain("az account show");
    choice.Detail.ShouldContain("Contoso Dev");
    return Task.CompletedTask;
  }

  public static Task EnvironmentSubscription_Should_WinAndNotBeOverridden()
  {
    AzureSubscriptionChoice choice = AspireDeployAzure.ChooseAzureSubscription(
      " 99999999-1111-2222-3333-444444444444 ", "88888888-1111-2222-3333-444444444444", CliAccount);
    choice.SubscriptionId.ShouldBe("99999999-1111-2222-3333-444444444444");
    choice.PassToAspire.ShouldBeFalse();
    choice.Detail.ShouldContain("Azure__SubscriptionId environment variable");
    return Task.CompletedTask;
  }

  public static Task UserSecretSubscription_Should_BeatTheAzCliAndBePassedToAspire()
  {
    // aspire deploy runs as Production and does not load user secrets, so the pinned subscription is
    // copied to Azure__SubscriptionId. The az CLI value is not the one passed.
    AzureSubscriptionChoice choice = AspireDeployAzure.ChooseAzureSubscription(null, "88888888-1111-2222-3333-444444444444", CliAccount);
    choice.SubscriptionId.ShouldBe("88888888-1111-2222-3333-444444444444");
    choice.PassToAspire.ShouldBeTrue();
    choice.Detail.ShouldContain("user secret Azure:SubscriptionId");
    choice.Detail.ShouldNotContain("az account show");
    choice.Detail.ShouldContain("does not load user secrets");
    return Task.CompletedTask;
  }

  public static Task SecretParameters_Should_BeCopiedToTheAspireEnvironmentWhenUnset()
  {
    Dictionary<string, string> environment = new()
    {
      [AspireDeployParameters.ParameterEnvironmentVariable(AspireDeploySecrets.NeonPooledConnectionStringParameter)] = "already",
    };
    Dictionary<string, string> aspireEnvironment = [];
    AspireDeployEnvironment.AddSecretParameterEnvironment(
      aspireEnvironment,
      [
        new ResolvedDeployParameter(AspireDeploySecrets.NeonPooledConnectionStringParameter, "from-env", "env"),
        new ResolvedDeployParameter(AspireDeploySecrets.NeonDirectConnectionStringParameter, "from-secret", "secret"),
      ],
      AspireDeployParameters.CaseInsensitiveEnvironment(environment));

    aspireEnvironment.ContainsKey(AspireDeployParameters.ParameterEnvironmentVariable(AspireDeploySecrets.NeonPooledConnectionStringParameter)).ShouldBeFalse();
    aspireEnvironment[AspireDeployParameters.ParameterEnvironmentVariable(AspireDeploySecrets.NeonDirectConnectionStringParameter)].ShouldBe("from-secret");
    return Task.CompletedTask;
  }

  public static Task AzureUserSecrets_Should_BeCopiedWhenTheEnvironmentIsUnset()
  {
    Dictionary<string, string> aspireEnvironment = [];
    const string output =
      "Azure:Location = eastus\nAzure:ResourceGroup = rg-example\nParameters:entra-client-secret = from-secret\n";
    AspireDeployEnvironment.AddAzureUserSecretEnvironment(
      aspireEnvironment,
      AspireDeployParameters.CaseInsensitiveEnvironment(new Dictionary<string, string>
      {
        [AspireDeployAzure.AzureLocationVariable] = "westus",
      }),
      true,
      output);

    aspireEnvironment.ContainsKey(AspireDeployAzure.AzureLocationVariable).ShouldBeFalse();
    aspireEnvironment[AspireDeployAzure.AzureResourceGroupVariable].ShouldBe("rg-example");
    aspireEnvironment[AspireDeployParameters.ParameterEnvironmentVariable(AspireDeployParameters.EntraClientSecretParameter)].ShouldBe("from-secret");
    return Task.CompletedTask;
  }

  public static Task UserSecretsList_Should_WrapTheDotnetTool()
  {
    AspireDeployParameters.BuildUserSecretsListArguments("/repo/app-host.csproj")
      .ShouldBe(["user-secrets", "list", "--project", "/repo/app-host.csproj"]);
    return Task.CompletedTask;
  }

  public static Task UserSecretsOutput_Should_YieldTheKeyCaseInsensitively()
  {
    const string output = "Parameters:postgres-password = p=a ss\nazure:subscriptionid = 88888888-1111-2222-3333-444444444444\nIngress:PublicUrl = https://x\n";
    AspireDeployParameters.ParseUserSecret(true, output, "Azure:SubscriptionId").ShouldBe("88888888-1111-2222-3333-444444444444");
    AspireDeployParameters.ParseUserSecret(true, output, "Parameters:postgres-password").ShouldBe("p=a ss");
    return Task.CompletedTask;
  }

  public static Task MissingOrFailedUserSecrets_Should_YieldNull()
  {
    AspireDeployParameters.ParseUserSecret(true, "No secrets configured for this application.\n", "Azure:SubscriptionId").ShouldBeNull();
    AspireDeployParameters.ParseUserSecret(false, "Azure:SubscriptionId = 88888888-1111-2222-3333-444444444444", "Azure:SubscriptionId").ShouldBeNull();
    AspireDeployParameters.ParseUserSecret(true, "Azure:SubscriptionId = ", "Azure:SubscriptionId").ShouldBeNull();
    AspireDeployParameters.ParseUserSecret(true, "Azure:SubscriptionIdOld = 1", "Azure:SubscriptionId").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task ContainerRuntime_Should_HonourAspireContainerRuntime()
  {
    AspireDeploy.ContainerRuntime(null).ShouldBe("docker");
    AspireDeploy.ContainerRuntime("podman").ShouldBe("podman");
    return Task.CompletedTask;
  }
}
