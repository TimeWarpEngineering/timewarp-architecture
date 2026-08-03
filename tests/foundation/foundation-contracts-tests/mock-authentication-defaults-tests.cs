#region Purpose
// Fail-closed activation matrix for mock auth (task 145-009).
// Duplicates the pure predicate logic of MockAuthenticationDefaults so foundation-contracts-tests
// stays free of a Web.Spa reference; keep in sync with MockAuthenticationDefaults.IsMockAuthActive.
#endregion

namespace MockAuthenticationDefaults_;

public class IsMockAuthActive_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsMockAuthActive_Given_>();

  // Mirror of TimeWarp.Architecture.Services.MockAuthenticationDefaults.IsMockAuthActive
  private static bool IsMockAuthActive(string? environmentName, string? useMockConfigurationValue)
  {
    if (string.IsNullOrEmpty(environmentName))
      return false;
    bool envOk = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
      || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    if (!envOk)
      return false;
    if (string.IsNullOrEmpty(useMockConfigurationValue))
      return false;
    return string.Equals(useMockConfigurationValue, "true", StringComparison.OrdinalIgnoreCase)
      || string.Equals(useMockConfigurationValue, "1", StringComparison.OrdinalIgnoreCase);
  }

  public static Task Production_With_Flag_True_Should_Be_False()
  {
    IsMockAuthActive("Production", "true").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Development_With_Flag_False_Should_Be_False()
  {
    IsMockAuthActive("Development", "false").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Development_With_Flag_True_Should_Be_True()
  {
    IsMockAuthActive("Development", "true").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Testing_With_Flag_True_Should_Be_True()
  {
    IsMockAuthActive("Testing", "true").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Staging_With_Flag_True_Should_Be_False()
  {
    IsMockAuthActive("Staging", "true").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Null_Environment_Should_Be_False()
  {
    IsMockAuthActive(null, "true").ShouldBeFalse();
    return Task.CompletedTask;
  }
}
