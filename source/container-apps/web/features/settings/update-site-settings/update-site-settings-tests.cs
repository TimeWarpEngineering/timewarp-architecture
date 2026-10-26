#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: UpdateSiteSettings contract + handler concurrency (task 219-006 / 227).
// Run standalone:  dotnet run source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs

#region Purpose
// Jaribu runfile: UpdateSiteSettings validator (passkey mode, accepted tenants),
// emptied store re-seeds at Version 0, 409, successful update. A policy save leaves the assistant name.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Settings
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Settings.UpdateSiteSettings;
  using UpdateHandler = TimeWarp.Architecture.Features.Settings.Application.UpdateSiteSettings.Handler;

  [TestTag("Contracts")]
  public class UpdateSiteSettingsCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateSiteSettingsCommand_Given_>();

    public static Task Undefined_Passkey_Mode_Should_Fail_Validation()
    {
      Command command = new()
      {
        PasskeyPromptMode = (PasskeyPromptMode)99,
        Version = 0
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Defined_Passkey_Mode_Should_Pass_Validation()
    {
      Command command = new()
      {
        EntraSignInEnabled = true,
        EntraAllowBootstrap = true,
        PasskeyPromptMode = PasskeyPromptMode.Required,
        Version = 0
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Empty_Accepted_Tenant_Should_Fail_Validation()
    {
      Command command = ValidCommand();
      command.EntraAcceptedTenants = [EntraAcceptedTenant.IdOnly(Guid.Empty)];
      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.ErrorMessage == "Accepted tenant ids must be non-empty GUIDs.");
      return Task.CompletedTask;
    }

    public static Task Duplicate_Accepted_Tenants_Past_The_Raw_Cap_Should_Pass_Validation()
    {
      Guid tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
      List<EntraAcceptedTenant> tenantIds = [];
      for (int index = 0; index < EntraTenantIds.MaxAcceptedCount + 1; index++)
      {
        tenantIds.Add(EntraAcceptedTenant.IdOnly(tenantId));
      }

      Command command = ValidCommand();
      command.EntraAcceptedTenants = tenantIds;
      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Too_Many_Distinct_Accepted_Tenants_Should_Fail_Validation()
    {
      List<EntraAcceptedTenant> tenantIds = [];
      for (int index = 1; index <= EntraTenantIds.MaxAcceptedCount + 1; index++)
      {
        tenantIds.Add(EntraAcceptedTenant.IdOnly(Guid.Parse($"00000000-0000-0000-0000-{index:D12}")));
      }

      Command command = ValidCommand();
      command.EntraAcceptedTenants = tenantIds;
      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.ErrorMessage == $"At most {EntraTenantIds.MaxAcceptedCount} accepted tenant ids.");
      return Task.CompletedTask;
    }

    public static Task Undefined_Tenant_Acceptance_Mode_Should_Fail_Validation()
    {
      Command command = ValidCommand();
      command.EntraTenantAcceptanceMode = (EntraTenantAcceptanceMode)99;
      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    private static Command ValidCommand() =>
      new()
      {
        PasskeyPromptMode = PasskeyPromptMode.Soft,
        Version = 0
      };
  }

  [TestTag("Application")]
  public class UpdateSiteSettingsHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateSiteSettingsHandler_Given_>();

    public static async Task Empty_Store_Should_Seed_Then_Update_At_Version_0()
    {
      InMemorySiteSettingsStore store = new();
      IOptions<EntraAuthenticationOptions> configured = Options.Create(new EntraAuthenticationOptions());
      UpdateHandler handler = new(
        new SeedOnReadSiteSettingsStore(
          store,
          new SiteSettingsSeeder(
            store,
            configured,
            Options.Create(new SignInOptions { Methods = [SignInMethod.Passkey] }),
            NullLogger<SiteSettingsSeeder>.Instance),
          isDevelopment: false,
          NullLogger<SeedOnReadSiteSettingsStore>.Instance));
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = true,
          EntraAllowBootstrap = true,
          PasskeyPromptMode = PasskeyPromptMode.Required,
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      result.AsT0.Version.ShouldBe(1);
      SiteSettings? stored = await store.GetAsync();
      stored.ShouldNotBeNull();
      stored.Version.ShouldBe(1);
    }

    public static async Task Stale_Version_Should_409()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      SiteSettings? current = await store.GetAsync();
      current!.ReplacePolicy(true, false, PasskeyPromptMode.Soft);
      await store.UpdateAsync(current);

      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = false,
          EntraAllowBootstrap = true,
          PasskeyPromptMode = PasskeyPromptMode.Required,
          Version = 0
        },
        default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(409);
      result.AsT1.Title.ShouldBe("Concurrency conflict");
      SiteSettings? stored = await store.GetAsync();
      stored!.EntraSignInEnabled.ShouldBeTrue();
    }

    public static async Task Matching_Version_Should_Persist_And_Advance()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = true,
          EntraAllowBootstrap = true,
          PasskeyPromptMode = PasskeyPromptMode.Required,
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.EntraAllowBootstrap.ShouldBeTrue();
      result.AsT0.Version.ShouldBe(1);
    }

    public static async Task Policy_Save_Should_Leave_The_Assistant_Name()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettings created = SiteSettings.Create();
      created.RenameAssistant("Zawadi");
      await store.AddAsync(created);
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          PasskeyPromptMode = PasskeyPromptMode.Soft,
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.AssistantName.ShouldBe("Zawadi");
      (await store.GetAsync())!.AssistantName.ShouldBe("Zawadi");
    }

    public static async Task Any_Mode_Should_Persist_The_Accepted_List()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      Guid first = Guid.Parse("00000000-0000-0000-0000-000000000001");
      Guid second = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          PasskeyPromptMode = PasskeyPromptMode.Soft,
          EntraTenantAcceptanceMode = EntraTenantAcceptanceMode.Any,
          EntraAcceptedTenants =
          [
            EntraAcceptedTenant.IdOnly(second),
            EntraAcceptedTenant.IdOnly(first),
            EntraAcceptedTenant.IdOnly(first),
          ],
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Any);
      result.AsT0.EntraAcceptedTenants.Count.ShouldBe(2);
      result.AsT0.EntraAcceptedTenants.ShouldContain(EntraAcceptedTenant.IdOnly(first));
      result.AsT0.EntraAcceptedTenants.ShouldContain(EntraAcceptedTenant.IdOnly(second));
      SiteSettings stored = (await store.GetAsync()).ShouldNotBeNull();
      stored.AcceptsEntraTenant(Guid.NewGuid()).ShouldBeTrue();
      stored.EntraTenantAcceptanceInitialized.ShouldBeTrue();
      stored.AssistantName.ShouldBe(AssistantNameRules.Default);
    }
  }
}
