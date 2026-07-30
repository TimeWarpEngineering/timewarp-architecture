#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu contract test, physically host-free (task 135, ported from the task 134
// spike). Duplicates the round-trip coverage of tests/container-apps/web/web-contracts-tests
// (role-contracts-serialization-tests.cs) for CreateRole, plus a Validator Name-rejection
// exercise, using ContractSerializationDefaults (the same seam options the SPA/server use).
// Run standalone:  dotnet run source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs

#region Purpose
// Jaribu runfile proving a co-located, standalone-runnable contract round-trip test for
// CreateRole (task 135; task 134 spike requirement 1).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

// Jaribu's SUT_/Action_Given_ naming convention requires underscores in type/member names
// (CA1707, via the NoWarn preamble above); it also requires a block-scoped namespace (top-level
// statements above cannot be followed by a file-scoped namespace — hence IDE0161 is in the same
// NoWarn list). Both are structural to the multi-mode runfile template, not a style lapse —
// matches the suppression already carried by the tests/ tree's Jaribu precedent
// (tests/foundation/foundation-domain-jaribu-tests/Directory.Build.props), expressed here as the
// standardized runfile preamble directive (task 135) since this file lives in a product folder
// alongside real slice code, not under tests/.
namespace TimeWarp.Architecture.Features.Admin.Roles
{

  using System.Text.Json;
  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class CreateRoleCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<CreateRoleCommand_Given_>();

    public static Task ValidCommand_Should_RoundTripThroughJson()
    {
      CreateRole.Command command = new()
      {
        UserId = Guid.NewGuid(),
        Name = "Auditor",
        Description = "Read-only access to financial modules."
      };

      string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      CreateRole.Command? parsed = JsonSerializer.Deserialize<CreateRole.Command>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.UserId.ShouldBe(command.UserId);
      parsed.Name.ShouldBe(command.Name);
      parsed.Description.ShouldBe(command.Description);
      return Task.CompletedTask;
    }
  }

  [TestTag("Contracts")]
  public class CreateRoleResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<CreateRoleResponse_Given_>();

    public static Task ValidRoleId_Should_RoundTripThroughJson()
    {
      CreateRole.Response response = new(RoleIds.Administrator);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      CreateRole.Response? parsed = JsonSerializer.Deserialize<CreateRole.Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.RoleId.ShouldBe(RoleIds.Administrator);
      return Task.CompletedTask;
    }

    public static Task EmptyGuidJson_Should_ThrowDuringDeserialization()
    {
      // The ctor Guard is the Response's invariant gate; deserialization must not bypass it.
      string json = """{"roleId":"00000000-0000-0000-0000-000000000000"}""";

      Should.Throw<Exception>(() =>
        JsonSerializer.Deserialize<CreateRole.Response>(json, ContractSerializationDefaults.Options));
      return Task.CompletedTask;
    }
  }

  [TestTag("Validation")]
  public class CreateRoleValidator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<CreateRoleValidator_Given_>();

    public static Task EmptyName_Should_FailValidation()
    {
      CreateRole.Command command = new()
      {
        UserId = Guid.NewGuid(),
        Name = "",
        Description = "Read-only access to financial modules."
      };

      ValidationResult result = new CreateRole.Validator().Validate(command);

      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateRole.Command.Name));
      return Task.CompletedTask;
    }

    public static Task ValidCommand_Should_PassValidation()
    {
      CreateRole.Command command = new()
      {
        UserId = Guid.NewGuid(),
        Name = "Auditor",
        Description = "Read-only access to financial modules."
      };

      ValidationResult result = new CreateRole.Validator().Validate(command);

      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Features.Admin.Roles
